# proto-engine

The prototype engine for the Astrea project. It uses the GraphHopper routing engine to calculate routes and distances between locations.

You will need to download the maps for the area you want to use. (It's too heavy to include in the repository.)
Either you go to https://download.geofabrik.de/europe.html and download the full France country (not recommanded) or you take a region you like.
After downloading the .osm.pbf file, you can put it in the `graphhopper/osm` folder. You also need to change the line 13 and 42 of the `docker-compose.yml` file to match the name of the .osm.pbf file you downloaded.

Instead of needing to remember all of the commands needed to run the project, you can use [just](https://github.com/casey/just) to run the project. All the commands are in the `justfile` and you can run them with `just <command>`.
If you don't know the commands, you can run `just --list` to see all the available commands.

## Run the docker

To run the docker, first you need to download the maps as explained above. Then you can run the following command to start the docker:

```bash
docker compose up -d
```
