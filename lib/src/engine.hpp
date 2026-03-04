#pragma once

extern "C" {

const char *astrea_route(const char *host, const char *points_csv, const char *user_json);
const char *astrea_health(const char *host);

} // extern "C"