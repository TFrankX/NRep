.PHONY: build build-linux clean

# Build for current platform
build:
	go build -o getid .

# Build for Linux (for deployment)
build-linux:
	GOOS=linux GOARCH=amd64 go build -o getid .

# Build smaller binary (strip debug info)
build-linux-small:
	GOOS=linux GOARCH=amd64 go build -ldflags="-s -w" -o getid .

clean:
	rm -f getid
