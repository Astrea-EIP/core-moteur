# proto-engine

The prototype engine for the Astrea project. It uses the GraphHopper routing engine to calculate routes and distances between locations.

You will need to download the maps for the area you want to use. (It's too heavy to include in the repository.)
Either you go to https://download.geofabrik.de/europe.html and download the full France country (not recommanded) or you take a region you like.

Instead of needing to remember all of the commands needed to run the project, you can use [just](https://github.com/casey/just) to run the project. All the commands are in the `justfile` and you can run them with `just <command>`.
If you don't know the commands, you can run `just --list` to see all the available commands.
## Running Python tests 📦

A small Python test suite exercises GraphHopper's HTTP API. It uses
`pytest` and `requests` and assumes a server is reachable at
`http://localhost:8989` (the default in `docker-compose.yml`).

```sh
pip install -r requirements.txt
pytest tests/test_graphhopper_routes.py
```

You can override the target URL with the `GRAPHHOPPER_URL`
environment variable, e.g.:

```sh
GRAPHHOPPER_URL=https://example.com pytest
```

