# getid - Vault Token Utility

Go implementation of the getid utility for managing Vault AppRole credentials.

## Build

```bash
# Build for current platform
make build

# Build for Linux (deployment)
make build-linux

# Build smaller binary
make build-linux-small
```

## Usage

### Store tokens

```bash
# Store Vault token
./getid setvlt <vault_token>

# Store Service Manager token
./getid setsm <sm_token>

# Store Consul token
./getid setcs <consul_token>
```

### Retrieve tokens

```bash
# Get Service Manager token
./getid getsm

# Get Consul token
./getid getcs
```

### Vault AppRole operations

```bash
# Get Role ID
./getid <vault_url>/v1 <role_name> roleid

# Get Secret ID (also clears old ones)
./getid <vault_url>/v1 <role_name> secid

# Clear old Secret IDs
./getid <vault_url>/v1 <role_name> clrid
```

### Examples

```bash
# Store vault token
./getid setvlt s.xxxxxxxxxxxxx

# Get role-id for project
./getid https://vault-prod.service.consul:8210/v1 myproject roleid

# Get secret-id for project
./getid https://vault-prod.service.consul:8210/v1 myproject secid
```

## Token storage

Tokens are stored encrypted in `~/.config/`:
- `tok` - Vault token
- `toksm` - Service Manager token
- `tokcs` - Consul token

Encryption uses AES-256-CBC with the network interface MAC address as the key.
