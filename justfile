# Start the containers, if "version" is set to "dev", then the docker-compose.dev.yml will be used
run version="prod": check
    #!/usr/bin/env bash
    if [ "{{version}}" = "dev" ]; then
        docker-compose -f docker-compose.dev.yml up -d
    else
        docker-compose up -d
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

logs: check
    docker-compose logs -f

# Download the latest OSM PBF files and place them in the "osm" directory
download-osm continent="europe" country="france" zone="":
    #!/usr/bin/env bash
    if [ -z "{{continent}}" ] || [ -z "{{country}}" ]; then
        echo "Error: continent and country variables must be set"
        exit 1
    fi
    mkdir -p osm
    if [ -z "{{zone}}" ]; then
        curl -L -o osm/{{country}}-latest.osm.pbf "https://download.geofabrik.de/{{continent}}/{{country}}-latest.osm.pbf"
    else
        curl -L -o osm/{{zone}}-latest.osm.pbf "https://download.geofabrik.de/{{continent}}/{{country}}/{{zone}}-latest.osm.pbf"
    fi

# Download OSM PBF file for France
download-osm-france:
    just download-osm "europe" "france"

# Download OSM PBF file for Pays de la Loire
download-osm-pdl:
    just download-osm "europe" "france" "pays-de-la-loire"

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

# Download OSM PBF file for Nord-Pas-de-Calais
download-osm-npc:
    just download-osm "europe" "france" "nord-pas-de-calais"