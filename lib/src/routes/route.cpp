#include <string>
#include <vector>
#include <nlohmann/json.hpp>

#include "../http_calls/http_calls.hpp"

namespace astrea {

std::string route(const std::string &host, std::vector<std::string> points, const std::string &user_json)
{
    if (points.size() < 2)
        return json_error("at least 2 points are required");

    nlohmann::json user;
    try {
        user = nlohmann::json::parse(user_json);
    } catch (...) {
        return json_error("Invalid user JSON");
    }

    std::string profile = user.value("profile","foot");
    std::string locale = user.value("locale", "en");
    bool ch_disable = user.value("ch_disable", false);

    std::string qs;
    for (const auto &p : points) {
        if (!qs.empty()) qs += '&';
        qs += "point=" + p;
    }
    qs += "&profile=" + profile;
    qs += "&locale="  + locale;
    if (ch_disable || profile != "foot")
        qs += "&ch.disable=true";

    return do_get(host, "/route?" + qs);
}

} // namespace astrea