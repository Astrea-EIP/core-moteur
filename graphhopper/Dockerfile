ARG GH_VERSION=11.0
FROM eclipse-temurin:17-jdk-jammy AS builder

ARG GH_VERSION

RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
RUN curl -L -o graphhopper-web.jar \
    "https://github.com/graphhopper/graphhopper/releases/download/${GH_VERSION}/graphhopper-web-${GH_VERSION}.jar"

FROM eclipse-temurin:17-jre-jammy
COPY --from=builder /app/graphhopper-web.jar /graphhopper-web.jar

WORKDIR /data
VOLUME ["/data"]

EXPOSE 8989

ENTRYPOINT ["sh", "-c", "exec java $JAVA_OPTS -jar /graphhopper-web.jar server \"$@\"", "--"]
