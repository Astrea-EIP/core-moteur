#include <string>
#include <nlohmann/json.hpp>
#include <httplib.h>
#include "http_calls.hpp"

std::string strip_scheme(const std::string &host)
{
    for (const auto &prefix : {"https://", "http://"}) {
        if (host.rfind(prefix, 0) == 0)
            return host.substr(std::string(prefix).size());
    }
    return host;
}

std::string json_error(const std::string &msg)
{
    return nlohmann::json{{"error", msg}}.dump();
}

std::string do_get(const std::string &host, const std::string &path, const std::string &body)
{
    httplib::Client client(strip_scheme(host));
    client.set_connection_timeout(5);
    client.set_read_timeout(10);

    httplib::Result res;
    if (body.empty()) {
        res = client.Get(path);
    } else {
        // httplib does not support GET with a body; POST is the correct
        // HTTP method when a request body is needed.
        res = client.Post(path, body, "application/json");
    }

    if (!res)
        return json_error(httplib::to_string(res.error()));
    if (res->status != 200)
        return json_error("HTTP " + std::to_string(res->status) + ": " + res->body);

    try {
        auto parsed = nlohmann::json::parse(res->body);
        (void)parsed;
        return res->body;
    } catch (...) {
        return nlohmann::json{{"status", res->body}}.dump();
    }
}