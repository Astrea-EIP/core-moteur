# proto-engine

The prototype engine for the Astrea project. It uses the GraphHopper routing engine to calculate routes and distances between locations.

You will need to download the maps for the area you want to use. (It's too heavy to include in the repository.)
Either you go to https://download.geofabrik.de/europe.html and download the full France country (not recommanded) or you take a region you like.

Instead of needing to remember all of the commands needed to run the project, you can use [just](https://github.com/casey/just) to run the project. All the commands are in the `justfile` and you can run them with `just <command>`.
If you don't know the commands, you can run `just --list` to see all the available commands.
