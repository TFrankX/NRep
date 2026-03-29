package main

import (
	"crypto/aes"
	"crypto/cipher"
	"crypto/rand"
	"crypto/sha256"
	"crypto/tls"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
	"path/filepath"
	"strconv"
	"strings"
	"time"
)

// Error codes
const (
	ErrNotEnoughParams = -1
	ErrWriteFile       = -2
	ErrCreateFile      = -3
	ErrReadFile        = -4
	ErrHTTPRequest     = -5
	ErrBadStatus       = -6
)

func main() {
	args := os.Args[1:]

	if len(args) == 0 {
		os.Exit(ErrNotEnoughParams)
	}

	// Get encryption key from machine identity
	key := deriveKey()

	// Handle commands
	switch args[0] {
	case "getsm":
		token, err := readAndDecrypt("toksm", key)
		if err != nil {
			os.Exit(ErrReadFile)
		}
		fmt.Print(token)
		return

	case "getcs":
		token, err := readAndDecrypt("tokcs", key)
		if err != nil {
			os.Exit(ErrReadFile)
		}
		fmt.Print(token)
		return

	case "gettc":
		token, err := readAndDecrypt("toktc", key)
		if err != nil {
			os.Exit(ErrReadFile)
		}
		fmt.Print(token)
		return

	case "getvlt":
		token, err := readAndDecrypt("tok", key)
		if err != nil {
			os.Exit(ErrReadFile)
		}
		fmt.Print(token)
		return

	case "setvlt":
		if len(args) < 2 {
			os.Exit(ErrNotEnoughParams)
		}
		err := encryptAndWrite("tok", args[1], key)
		if err != nil {
			fmt.Println("Failed to write to file")
			os.Exit(ErrCreateFile)
		}
		fmt.Println("Vault token saved")
		return

	case "setsm":
		if len(args) < 2 {
			os.Exit(ErrNotEnoughParams)
		}
		err := encryptAndWrite("toksm", args[1], key)
		if err != nil {
			fmt.Println("Failed to write to file")
			os.Exit(ErrCreateFile)
		}
		fmt.Println("Service Manager token saved")
		return

	case "setcs":
		if len(args) < 2 {
			os.Exit(ErrNotEnoughParams)
		}
		err := encryptAndWrite("tokcs", args[1], key)
		if err != nil {
			fmt.Println("Failed to write to file")
			os.Exit(ErrCreateFile)
		}
		fmt.Println("Consul token saved")
		return

	case "settc":
		// Set TeamCity credentials: settc <user:password> or settc <user> <password>
		if len(args) < 2 {
			fmt.Println("Usage: getid settc <user:password> or getid settc <user> <password>")
			os.Exit(ErrNotEnoughParams)
		}
		var tcCreds string
		if len(args) >= 3 {
			// settc <user> <password>
			tcCreds = args[1] + ":" + args[2]
		} else {
			// settc <user:password>
			tcCreds = args[1]
		}
		err := encryptAndWrite("toktc", tcCreds, key)
		if err != nil {
			fmt.Println("Failed to write to file")
			os.Exit(ErrCreateFile)
		}
		fmt.Println("TeamCity credentials saved")
		return

	case "debug":
		fmt.Printf("MAC address: %s\n", getMACAddress())
		fmt.Printf("Hostname:    %s\n", getHostname())
		fmt.Printf("Username:    %s\n", getUsername())
		fmt.Printf("Key (hex):   %x\n", key)
		return

	case "setsec":
		// Set arbitrary secret: setsec <name> <secret>
		if len(args) < 3 {
			fmt.Println("Usage: getid setsec <name> <secret>")
			os.Exit(ErrNotEnoughParams)
		}
		secretName := "sec_" + args[1]
		secretValue := args[2]
		err := encryptAndWrite(secretName, secretValue, key)
		if err != nil {
			fmt.Println("Failed to write to file")
			os.Exit(ErrCreateFile)
		}
		fmt.Printf("Secret '%s' saved\n", args[1])
		return

	case "getsec":
		// Get arbitrary secret: getsec <name>
		if len(args) < 2 {
			fmt.Println("Usage: getid getsec <name>")
			os.Exit(ErrNotEnoughParams)
		}
		secretName := "sec_" + args[1]
		secret, err := readAndDecrypt(secretName, key)
		if err != nil {
			os.Exit(ErrReadFile)
		}
		fmt.Print(secret)
		return

	case "listsec":
		// List all custom secrets
		listSecrets()
		return

	case "setssh":
		// Encrypt SSH key: setssh <keyname>
		// Reads from ~/.ssh/<keyname>, encrypts to ~/.config/store/transport/<keyname>
		if len(args) < 2 {
			fmt.Println("Usage: getid setssh <keyname>")
			os.Exit(ErrNotEnoughParams)
		}
		keyName := args[1]
		if err := encryptSSHKey(keyName, key); err != nil {
			fmt.Printf("Error: %v\n", err)
			os.Exit(ErrCreateFile)
		}
		fmt.Printf("SSH key '%s' encrypted successfully\n", keyName)
		return

	case "getssh":
		// Decrypt SSH key: getssh <keyname>
		// Outputs decrypted key to stdout
		if len(args) < 2 {
			fmt.Println("Usage: getid getssh <keyname>")
			os.Exit(ErrNotEnoughParams)
		}
		keyName := args[1]
		keyData, err := decryptSSHKey(keyName, key)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(ErrReadFile)
		}
		fmt.Print(keyData)
		return

	case "listssh":
		// List encrypted SSH keys
		listSSHKeys()
		return

	case "setsshall":
		// Encrypt all SSH keys from ~/.ssh
		count, errors := encryptAllSSHKeys(key)
		fmt.Printf("Encrypted: %d keys\n", count)
		if len(errors) > 0 {
			fmt.Printf("Errors: %d\n", len(errors))
			for _, e := range errors {
				fmt.Printf("  - %s\n", e)
			}
		}
		return
	}

	// For roleid, secid, clrid - need at least 3 params
	if len(args) < 3 {
		os.Exit(ErrNotEnoughParams)
	}

	vaultURL := args[0]
	roleName := args[1]
	command := args[2]

	// Read vault token
	vaultToken, err := readAndDecrypt("tok", key)
	if err != nil {
		os.Exit(ErrReadFile)
	}

	client := createHTTPClient()

	switch command {
	case "roleid":
		roleID, err := getRoleID(client, vaultURL, roleName, vaultToken)
		if err != nil {
			os.Exit(ErrBadStatus)
		}
		fmt.Print(roleID)

	case "secid":
		// Clear old secrets first (keep 1)
		clearSecretsKeepN(client, vaultURL, roleName, vaultToken, 1)

		// Generate new secret-id
		secretID, err := getSecretID(client, vaultURL, roleName, vaultToken)
		if err != nil {
			os.Exit(ErrBadStatus)
		}
		fmt.Print(secretID)

	case "clrid":
		// Optional 4th arg: number of secrets to keep (default 1)
		keepCount := 1
		if len(args) > 3 {
			if n, err := strconv.Atoi(args[3]); err == nil && n > 0 {
				keepCount = n
			}
		}
		count := clearSecretsKeepN(client, vaultURL, roleName, vaultToken, keepCount)
		fmt.Printf("Cleared:%d\n", count)
	}
}

// getMACAddress returns the MAC address of the first suitable network interface
func getMACAddress() string {
	interfaces := []string{
		"/sys/class/net/ens160/address",
		"/sys/class/net/eth0/address",
		"/sys/class/net/ens192/address",
		"/sys/class/net/ens33/address",
	}

	for _, iface := range interfaces {
		data, err := os.ReadFile(iface)
		if err == nil {
			return strings.TrimSpace(string(data))
		}
	}

	// Fallback: try to find any network interface
	entries, err := os.ReadDir("/sys/class/net")
	if err == nil {
		for _, entry := range entries {
			if entry.Name() == "lo" {
				continue
			}
			addrPath := filepath.Join("/sys/class/net", entry.Name(), "address")
			data, err := os.ReadFile(addrPath)
			if err == nil {
				mac := strings.TrimSpace(string(data))
				if mac != "" && mac != "00:00:00:00:00:00" {
					return mac
				}
			}
		}
	}

	return ""
}

// deriveKey creates a 32-byte AES key from machine identity using SHA-256
// Components: MAC address + hostname + username
func deriveKey() []byte {
	mac := getMACAddress()
	hostname := getHostname()
	username := getUsername()

	// Combine all identity components with salt
	identity := fmt.Sprintf("getid-v2:%s:%s:%s:secure-token-storage", mac, hostname, username)
	hash := sha256.Sum256([]byte(identity))
	return hash[:]
}

// getHostname returns the machine hostname
func getHostname() string {
	hostname, err := os.Hostname()
	if err != nil {
		return "unknown-host"
	}
	return hostname
}

// getUsername returns the current username
func getUsername() string {
	// Try environment variables first
	if user := os.Getenv("USER"); user != "" {
		return user
	}
	if user := os.Getenv("LOGNAME"); user != "" {
		return user
	}
	// Fallback: get from home directory
	if home, err := os.UserHomeDir(); err == nil {
		return filepath.Base(home)
	}
	return "unknown-user"
}

// getStorePath returns the path to the token storage directory
func getStorePath() string {
	homeDir, _ := os.UserHomeDir()
	return filepath.Join(homeDir, ".config", "store")
}

// readAndDecrypt reads a token file and decrypts it
func readAndDecrypt(filename string, key []byte) (string, error) {
	filePath := filepath.Join(getStorePath(), filename)

	data, err := os.ReadFile(filePath)
	if err != nil {
		return "", err
	}

	encrypted := strings.TrimSpace(string(data))
	return decrypt(encrypted, key)
}

// encryptAndWrite encrypts a value and writes it to a token file
func encryptAndWrite(filename, value string, key []byte) error {
	storeDir := getStorePath()

	// Ensure store directory exists
	if err := os.MkdirAll(storeDir, 0700); err != nil {
		return err
	}

	filePath := filepath.Join(storeDir, filename)

	// Delete existing file
	os.Remove(filePath)

	encrypted, err := encrypt(value, key)
	if err != nil {
		return err
	}

	return os.WriteFile(filePath, []byte(encrypted), 0600)
}

// getTransportPath returns the path to SSH keys storage
func getTransportPath() string {
	homeDir, _ := os.UserHomeDir()
	return filepath.Join(homeDir, ".config", "store", "transport")
}

// getSSHPath returns the path to ~/.ssh
func getSSHPath() string {
	homeDir, _ := os.UserHomeDir()
	return filepath.Join(homeDir, ".ssh")
}

// SSH key prefixes to try when looking for unencrypted keys
var sshKeyPrefixes = []string{"id_rsa_", "id_ed25519_", "id_ecdsa_", "id_dsa_"}

// stripKeyPrefix removes common SSH key prefixes from key name
// e.g., "id_rsa_deploy" -> "deploy", "id_ed25519_mykey" -> "mykey"
func stripKeyPrefix(keyName string) string {
	for _, prefix := range sshKeyPrefixes {
		if strings.HasPrefix(keyName, prefix) {
			return strings.TrimPrefix(keyName, prefix)
		}
	}
	return keyName
}

// getUnencryptedKeyPath tries to find unencrypted key in ~/.ssh
// Returns path and true if found, empty string and false otherwise
func getUnencryptedKeyPath(keyName string) (string, bool) {
	sshDir := getSSHPath()

	// First try with common prefixes (id_rsa_keyname, id_ed25519_keyname, etc.)
	for _, prefix := range sshKeyPrefixes {
		path := filepath.Join(sshDir, prefix+keyName)
		if _, err := os.Stat(path); err == nil {
			return path, true
		}
	}

	// Then try as-is (for keys like "id_rsa", "id_ed25519", or custom names)
	path := filepath.Join(sshDir, keyName)
	if _, err := os.Stat(path); err == nil {
		return path, true
	}

	return "", false
}

// encryptSSHKey encrypts an SSH key from ~/.ssh to transport storage
// Encrypted keys are stored WITHOUT prefix (id_rsa_deploy -> deploy)
func encryptSSHKey(keyName string, key []byte) error {
	// Read from ~/.ssh (with original name)
	sshPath := filepath.Join(getSSHPath(), keyName)
	keyData, err := os.ReadFile(sshPath)
	if err != nil {
		return fmt.Errorf("cannot read SSH key '%s': %v", sshPath, err)
	}

	// Ensure transport directory exists
	transportDir := getTransportPath()
	if err := os.MkdirAll(transportDir, 0700); err != nil {
		return fmt.Errorf("cannot create transport directory: %v", err)
	}

	// Encrypt
	encrypted, err := encrypt(string(keyData), key)
	if err != nil {
		return fmt.Errorf("encryption failed: %v", err)
	}

	// Strip prefix for storage (id_rsa_deploy -> deploy)
	storageName := stripKeyPrefix(keyName)

	// Write to transport
	transportPath := filepath.Join(transportDir, storageName)
	if err := os.WriteFile(transportPath, []byte(encrypted), 0600); err != nil {
		return fmt.Errorf("cannot write encrypted key: %v", err)
	}

	return nil
}

// decryptSSHKey decrypts an SSH key from transport storage
// Search order:
// 1. ~/.ssh with prefixes (id_rsa_<keyName>, id_ed25519_<keyName>, etc.)
// 2. ~/.ssh as-is (<keyName>)
// 3. transport/<keyName> (encrypted, already without prefix)
func decryptSSHKey(keyName string, key []byte) (string, error) {
	// Strip prefix if provided (normalize the key name)
	normalizedName := stripKeyPrefix(keyName)

	// First try ~/.ssh (unencrypted) with various prefixes
	if sshPath, found := getUnencryptedKeyPath(normalizedName); found {
		if data, err := os.ReadFile(sshPath); err == nil {
			return string(data), nil
		}
	}

	// Also try the original keyName as-is in ~/.ssh (for backwards compatibility)
	if keyName != normalizedName {
		sshPath := filepath.Join(getSSHPath(), keyName)
		if data, err := os.ReadFile(sshPath); err == nil {
			return string(data), nil
		}
	}

	// Try transport (encrypted) - stored without prefix
	transportPath := filepath.Join(getTransportPath(), normalizedName)
	data, err := os.ReadFile(transportPath)
	if err != nil {
		return "", fmt.Errorf("SSH key '%s' not found in ~/.ssh or transport", keyName)
	}

	// Decrypt
	decrypted, err := decrypt(strings.TrimSpace(string(data)), key)
	if err != nil {
		return "", fmt.Errorf("decryption failed: %v", err)
	}

	return decrypted, nil
}

// listSSHKeys lists all encrypted SSH keys in transport
func listSSHKeys() {
	transportDir := getTransportPath()
	entries, err := os.ReadDir(transportDir)
	if err != nil {
		fmt.Println("No encrypted SSH keys found")
		return
	}

	fmt.Println("Encrypted SSH keys (use 'getssh <name>' to decrypt):")
	for _, entry := range entries {
		if !entry.IsDir() {
			fmt.Printf("  %s\n", entry.Name())
		}
	}
}

// listSecrets lists all custom secrets (files starting with "sec_")
func listSecrets() {
	storeDir := getStorePath()
	entries, err := os.ReadDir(storeDir)
	if err != nil {
		fmt.Println("No secrets found")
		return
	}

	found := false
	for _, entry := range entries {
		if !entry.IsDir() && strings.HasPrefix(entry.Name(), "sec_") {
			if !found {
				fmt.Println("Custom secrets (use 'getsec <name>' to decrypt):")
				found = true
			}
			// Remove "sec_" prefix for display
			name := strings.TrimPrefix(entry.Name(), "sec_")
			fmt.Printf("  %s\n", name)
		}
	}

	if !found {
		fmt.Println("No custom secrets found")
	}
}

// isPrivateKeyFile checks if a file looks like an SSH private key
func isPrivateKeyFile(name string) bool {
	// Skip known non-key files
	skipFiles := map[string]bool{
		"known_hosts":     true,
		"known_hosts.old": true,
		"config":          true,
		"authorized_keys": true,
		"environment":     true,
	}

	if skipFiles[name] {
		return false
	}

	// Skip public keys
	if strings.HasSuffix(name, ".pub") {
		return false
	}

	// Skip directories and hidden files starting with .
	if strings.HasPrefix(name, ".") {
		return false
	}

	return true
}

// encryptAllSSHKeys encrypts all SSH private keys from ~/.ssh
// Keys are stored WITHOUT prefix (id_rsa_deploy -> deploy)
func encryptAllSSHKeys(key []byte) (int, []string) {
	sshDir := getSSHPath()
	entries, err := os.ReadDir(sshDir)
	if err != nil {
		return 0, []string{fmt.Sprintf("Cannot read ~/.ssh: %v", err)}
	}

	var errors []string
	count := 0

	for _, entry := range entries {
		if entry.IsDir() {
			continue
		}

		name := entry.Name()
		if !isPrivateKeyFile(name) {
			continue
		}

		// Check if file content looks like a private key
		filePath := filepath.Join(sshDir, name)
		content, err := os.ReadFile(filePath)
		if err != nil {
			continue
		}

		// Private keys start with "-----BEGIN"
		if !strings.HasPrefix(string(content), "-----BEGIN") {
			continue
		}

		// Encrypt the key (will be stored without prefix)
		storageName := stripKeyPrefix(name)
		if err := encryptSSHKey(name, key); err != nil {
			errors = append(errors, fmt.Sprintf("%s: %v", name, err))
		} else {
			fmt.Printf("  Encrypted: %s -> %s\n", name, storageName)
			count++
		}
	}

	return count, errors
}

// encrypt encrypts text using AES-256-GCM (authenticated encryption)
// Format: base64(nonce || ciphertext || tag)
func encrypt(plaintext string, key []byte) (string, error) {
	block, err := aes.NewCipher(key)
	if err != nil {
		return "", err
	}

	gcm, err := cipher.NewGCM(block)
	if err != nil {
		return "", err
	}

	// Generate random nonce
	nonce := make([]byte, gcm.NonceSize())
	if _, err := io.ReadFull(rand.Reader, nonce); err != nil {
		return "", err
	}

	// Encrypt and authenticate
	ciphertext := gcm.Seal(nonce, nonce, []byte(plaintext), nil)

	return base64.StdEncoding.EncodeToString(ciphertext), nil
}

// decrypt decrypts text using AES-256-GCM
func decrypt(encrypted string, key []byte) (string, error) {
	data, err := base64.StdEncoding.DecodeString(encrypted)
	if err != nil {
		return "", err
	}

	block, err := aes.NewCipher(key)
	if err != nil {
		return "", err
	}

	gcm, err := cipher.NewGCM(block)
	if err != nil {
		return "", err
	}

	if len(data) < gcm.NonceSize() {
		return "", fmt.Errorf("ciphertext too short")
	}

	nonce := data[:gcm.NonceSize()]
	ciphertext := data[gcm.NonceSize():]

	plaintext, err := gcm.Open(nil, nonce, ciphertext, nil)
	if err != nil {
		return "", err
	}

	return string(plaintext), nil
}

// createHTTPClient creates an HTTP client that skips SSL verification
func createHTTPClient() *http.Client {
	tr := &http.Transport{
		TLSClientConfig: &tls.Config{InsecureSkipVerify: true},
	}
	return &http.Client{
		Transport: tr,
		Timeout:   30 * time.Second,
	}
}

// getRoleID gets the role-id from Vault
func getRoleID(client *http.Client, vaultURL, roleName, token string) (string, error) {
	url := fmt.Sprintf("%s/auth/approle/role/%s/role-id", vaultURL, roleName)

	req, err := http.NewRequest("GET", url, nil)
	if err != nil {
		return "", err
	}

	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("X-Vault-Token", token)

	resp, err := client.Do(req)
	if err != nil {
		return "", err
	}
	defer resp.Body.Close()

	if resp.StatusCode != 200 {
		return "", fmt.Errorf("bad status: %d", resp.StatusCode)
	}

	body, _ := io.ReadAll(resp.Body)

	var result struct {
		Data struct {
			RoleID string `json:"role_id"`
		} `json:"data"`
	}

	if err := json.Unmarshal(body, &result); err != nil {
		return "", err
	}

	return result.Data.RoleID, nil
}

// getSecretID generates a new secret-id from Vault
func getSecretID(client *http.Client, vaultURL, roleName, token string) (string, error) {
	url := fmt.Sprintf("%s/auth/approle/role/%s/secret-id", vaultURL, roleName)

	req, err := http.NewRequest("PUT", url, nil)
	if err != nil {
		return "", err
	}

	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("X-Vault-Token", token)

	resp, err := client.Do(req)
	if err != nil {
		return "", err
	}
	defer resp.Body.Close()

	if resp.StatusCode != 200 {
		return "", fmt.Errorf("bad status: %d", resp.StatusCode)
	}

	body, _ := io.ReadAll(resp.Body)

	var result struct {
		Data struct {
			SecretID string `json:"secret_id"`
		} `json:"data"`
	}

	if err := json.Unmarshal(body, &result); err != nil {
		return "", err
	}

	return result.Data.SecretID, nil
}

// clearSecretsKeepN removes old secret-id accessors, keeping the N newest ones
func clearSecretsKeepN(client *http.Client, vaultURL, roleName, token string, keepCount int) int {
	if keepCount < 1 {
		keepCount = 1
	}

	// List all secret-id accessors
	url := fmt.Sprintf("%s/auth/approle/role/%s/secret-id?list=true", vaultURL, roleName)

	req, _ := http.NewRequest("GET", url, nil)
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("X-Vault-Token", token)

	resp, err := client.Do(req)
	if err != nil {
		return 0
	}
	defer resp.Body.Close()

	if resp.StatusCode != 200 {
		return 0
	}

	body, _ := io.ReadAll(resp.Body)

	var listResult struct {
		Data struct {
			Keys []string `json:"keys"`
		} `json:"data"`
	}

	if err := json.Unmarshal(body, &listResult); err != nil {
		return 0
	}

	if len(listResult.Data.Keys) <= keepCount {
		return 0
	}

	// Get creation time for each accessor
	type accessorInfo struct {
		accessor     string
		creationTime time.Time
	}

	var accessors []accessorInfo

	for _, accessor := range listResult.Data.Keys {
		lookupURL := fmt.Sprintf("%s/auth/approle/role/%s/secret-id-accessor/lookup", vaultURL, roleName)
		payload := fmt.Sprintf(`{"secret_id_accessor":"%s"}`, accessor)

		req, _ := http.NewRequest("POST", lookupURL, strings.NewReader(payload))
		req.Header.Set("Content-Type", "application/json")
		req.Header.Set("X-Vault-Token", token)

		resp, err := client.Do(req)
		if err != nil {
			continue
		}

		if resp.StatusCode != 200 && resp.StatusCode != 204 {
			resp.Body.Close()
			continue
		}

		body, _ := io.ReadAll(resp.Body)
		resp.Body.Close()

		var lookupResult struct {
			Data struct {
				CreationTime string `json:"creation_time"`
			} `json:"data"`
		}

		if err := json.Unmarshal(body, &lookupResult); err != nil {
			continue
		}

		// Parse creation time
		timeStr := lookupResult.Data.CreationTime
		if idx := strings.Index(timeStr, "."); idx > 0 {
			timeStr = timeStr[:idx]
		}

		t, err := time.Parse("2006-01-02T15:04:05", timeStr)
		if err != nil {
			continue
		}

		accessors = append(accessors, accessorInfo{
			accessor:     accessor,
			creationTime: t,
		})
	}

	if len(accessors) <= keepCount {
		return 0
	}

	// Sort by creation time (newest first)
	for i := 0; i < len(accessors)-1; i++ {
		for j := i + 1; j < len(accessors); j++ {
			if accessors[j].creationTime.After(accessors[i].creationTime) {
				accessors[i], accessors[j] = accessors[j], accessors[i]
			}
		}
	}

	// Delete all except the keepCount newest
	count := 0
	for i := keepCount; i < len(accessors); i++ {
		a := accessors[i]

		destroyURL := fmt.Sprintf("%s/auth/approle/role/%s/secret-id-accessor/destroy", vaultURL, roleName)
		payload := fmt.Sprintf(`{"secret_id_accessor":"%s"}`, a.accessor)

		req, _ := http.NewRequest("POST", destroyURL, strings.NewReader(payload))
		req.Header.Set("Content-Type", "application/json")
		req.Header.Set("X-Vault-Token", token)

		resp, err := client.Do(req)
		if err != nil {
			continue
		}
		resp.Body.Close()

		if resp.StatusCode == 200 || resp.StatusCode == 204 {
			count++
		}
	}

	return count
}

