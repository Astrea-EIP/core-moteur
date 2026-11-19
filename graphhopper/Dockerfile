# Stage 1: Build the JAR from source
FROM eclipse-temurin:17-jdk-jammy AS builder

RUN apt-get update && \
    apt-get install -y --no-install-recommends git maven && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

RUN git clone --depth 1 --branch 10.2 https://github.com/graphhopper/graphhopper.git .

RUN mvn clean package -DskipTests -Pskip-format

# Stage 2: Engine image (JAR + config + custom models, without the graph).
# This is the default target: `docker build graphhopper` and the compose files use it.
# The published image adds the pre-built graph on top (see Dockerfile.graph).
FROM eclipse-temurin:17-jre-jammy
COPY --from=builder /app/web/target/graphhopper-web-*.jar /graphhopper-web.jar
COPY config.yml /config.yml
COPY custom_models/ /data/custom_models/

# SerialGC is forced on purpose: with a small heap the JVM would pick G1 as soon as
# the container limit exceeds ~1.8 GB, and G1 runs out of memory at -Xmx1024m.
ENV JAVA_OPTS="-Xmx1024m -XX:+UseSerialGC"

# No VOLUME on /data: an anonymous volume would hide the graph embedded in the image.
WORKDIR /data

EXPOSE 8989

ENTRYPOINT ["sh", "-c", "exec java $JAVA_OPTS -jar /graphhopper-web.jar server \"$@\"", "--"]
CMD ["/config.yml"]
