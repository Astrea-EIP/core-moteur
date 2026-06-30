# Stage 1: Build the JAR from source
FROM eclipse-temurin:17-jdk-jammy AS builder

RUN apt-get update && \
    apt-get install -y --no-install-recommends git maven && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

RUN git clone --depth 1 --branch 10.2 https://github.com/graphhopper/graphhopper.git .

RUN mvn clean package -DskipTests -Pskip-format

FROM eclipse-temurin:17-jre-jammy
COPY --from=builder /app/web/target/graphhopper-web-*.jar /graphhopper-web.jar

WORKDIR /data
VOLUME ["/data"]

EXPOSE 8989

ENTRYPOINT ["sh", "-c", "exec java $JAVA_OPTS -jar /graphhopper-web.jar server \"$@\"", "--"]