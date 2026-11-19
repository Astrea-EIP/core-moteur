---
title: Setup
sidebar_position: 2
---

# Setup

## Commands

```bash
dotnet build lib -c Release
dotnet test tests --configuration Release
```

## GraphHopper image

Each core-moteur tag publishes `ghcr.io/astrea-eip/graphhopper:vX.Y.Z` (and `latest`). The image is self-contained: it embeds `config.yml`, the `custom_models/` and the graph already built from the Pays de la Loire OSM extract. A server only needs the image, with no core-moteur clone, no `.osm.pbf` file and no import at startup.

- `graphhopper/Dockerfile` builds the engine image (JAR, config, custom models). It is also what the compose files and the pull request check build.
- `graphhopper/Dockerfile.graph` adds the graph on top of it. Only the publish workflow uses it.
- The publish workflow downloads the OSM extract, checks its MD5, runs `import`, builds the final image, runs a smoke test without any mount, then pushes. If the import or the smoke test fails, nothing is published.
- The image carries OCI labels: core-moteur tag, GraphHopper version, and the date and MD5 of the OSM extract.
- Default `JAVA_OPTS` is `-Xmx1024m -XX:+UseSerialGC`. Keep SerialGC: with G1 the JVM runs out of memory at this heap size.
- Do not mount a volume on `/data/graphhopper` or `/data`: it would hide the embedded graph. The graph must also stay writable (GraphHopper creates `gh.lock` in it).
- Updating the map means publishing a new core-moteur tag.

## Validation

Run build and tests before opening a pull request.
