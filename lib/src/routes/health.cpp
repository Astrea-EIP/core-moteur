#include "../http_calls/http_calls.hpp"

#include <string>

namespace astrea {

std::string health(const std::string &host) {
    return do_get(host, "/health");
}

} // namespace astrea
