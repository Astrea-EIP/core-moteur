#pragma once

#include <string>

std::string strip_scheme(const std::string &host);
std::string json_error(const std::string &msg);

/// Performs a GET request when body is empty, POST otherwise.
/// Always returns a JSON string.
std::string do_get(const std::string &host, const std::string &path, const std::string &body = "");