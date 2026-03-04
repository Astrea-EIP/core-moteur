#pragma once

#include <string>
#include <vector>

namespace astrea {

/// Calls GraphHopper GET /route and returns the raw JSON response body.
std::string route(const std::string &host, std::vector<std::string> points, const std::string &user_json);

/// Calls GraphHopper GET /health and returns the raw JSON response body.
std::string health(const std::string &host);

} // namespace astrea