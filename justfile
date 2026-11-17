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

# Run unit tests only (no GraphHopper container needed)
test-unit:
    cd lib && dotnet build
    cd tests && dotnet test --filter "Category!=Integration"

# Run integration tests only (requires GraphHopper)
test-integration:
    cd lib && dotnet build
    cd tests && dotnet test --filter "Category=Integration"

# Run lightweight GraphHopper for integration tests (Nantes extract)
run-test-gh: check
    #!/usr/bin/env bash
    set -euo pipefail
    docker compose -f docker-compose.test.yml up -d
    echo "Waiting for GraphHopper to be ready..."
    timeout=60
    while [ $timeout -gt 0 ]; do
        if curl -s http://localhost:8989/health | grep -q 'OK'; then
            echo "GraphHopper is ready at http://localhost:8989"
            exit 0
        fi
        sleep 1
        timeout=$((timeout - 1))
    done
    echo "Error: GraphHopper timed out after 60s"
    docker compose -f docker-compose.test.yml logs graphhopper
    exit 1

# Stop lightweight GraphHopper test container
stop-test-gh: check
    docker compose -f docker-compose.test.yml down

# Clean test artifacts
clean-test:
    cd tests && dotnet clean && rm -rf bin obj

# Run the interactive web playground to test routing with custom userJson
playground:
    #!/usr/bin/env bash
    URL="http://localhost:5050"
    if command -v xdg-open > /dev/null 2>&1; then
        (sleep 1 && xdg-open "$URL") &
    elif command -v open > /dev/null 2>&1; then
        (sleep 1 && open "$URL") &
    elif [ -n "${COMSPEC:-}" ] || command -v cmd.exe > /dev/null 2>&1; then
        (sleep 1 && cmd.exe /c start "$URL") &
    else
        echo "Playground server starting. Open $URL in your browser."
    fi
    cd playground && dotnet run
