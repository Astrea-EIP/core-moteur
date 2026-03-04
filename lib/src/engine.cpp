#include "engine.hpp"
#include "routes/routes_list.hpp"

extern "C" {

/// Route request.
/// points_csv : pipe-separated "lat,lon" pairs, e.g. "47.21,-1.55|47.22,-1.54"
/// user_json  : JSON object describing the person, e.g. {"profile":"disabled","locale":"fr"}
/// Returns a JSON string (static storage — not thread-safe).
const char *astrea_route(const char *host, const char *points_csv, const char *user_json)
{
    static std::string result;
    std::vector<std::string> points;

    std::string csv = points_csv ? points_csv : "";
    std::string::size_type start = 0, end;
    while ((end = csv.find('|', start)) != std::string::npos) {
        points.push_back(csv.substr(start, end - start));
        start = end + 1;
    }
    if (start < csv.size())
        points.push_back(csv.substr(start));

    result = astrea::route(host, points, user_json);
    return result.c_str();
}

/// Health check.
const char *astrea_health(const char *host)
{
    static std::string result;
    result = astrea::health(host ? host : "");
    return result.c_str();
}

} // extern "C"

