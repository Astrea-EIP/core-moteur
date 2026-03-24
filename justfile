# --- Docker ---

# Start the containers, if "version" is set to "dev", then the docker-compose.dev.yml will be used
run version="prod": check
    #!/usr/bin/env bash
    if [ "{{version}}" = "dev" ]; then
        docker-compose -f docker-compose.dev.yml up
    else
        docker-compose up
    fi

# Check if docker and docker-compose are installed
check:
    @command -v docker > /dev/null 2>&1 || (echo "Error: docker is not installed" && exit 1)
    @command -v docker-compose > /dev/null 2>&1 || (echo "Error: docker-compose is not installed" && exit 1)

# Stop the containers
stop: check
    docker-compose down

# Stop the contains and remove the volumes
clean: check
    docker-compose down -v

# Run with a fresh image
build: check
    docker-compose build --no-cache
    just run

# --- C# library ---

# Build the C# library (debug)
build-lib:
    cd lib && dotnet build

# Build the C# library (release)
build-lib-release:
    cd lib && dotnet build -c Release

# Clean C# build artifacts
clean-lib:
    cd lib && dotnet clean && rm -rf bin obj

# Build and run C# tests (tests reference the compiled DLL)
test-lib:
    cd lib && dotnet build
    cd tests && dotnet test

# Clean test artifacts
clean-test:
    cd tests && dotnet clean && rm -rf bin obj