#!/bin/sh
set -eu

hub_url="${EXCHANGE_HUB_URL:-http://localhost:5000/hubs/exchange}"
ml_api_url="${ML_API_URL:-http://localhost:5005/api/v1/}"

validate_url() {
  variable_name="$1"
  value="$2"

  case "$value" in
    //*)
      echo "$variable_name must not use a protocol-relative URL." >&2
      exit 1
      ;;
    http://* | https://* | /*) ;;
    *)
      echo "$variable_name must be an HTTP(S) URL or a same-origin path." >&2
      exit 1
      ;;
  esac

  case "$value" in
    *'"'* | *'\'* | *"
"*)
      echo "$variable_name contains unsupported characters." >&2
      exit 1
      ;;
  esac
}

validate_url "EXCHANGE_HUB_URL" "$hub_url"
validate_url "ML_API_URL" "$ml_api_url"

printf '{\n  "hubUrl": "%s",\n  "mlApiUrl": "%s"\n}\n' "$hub_url" "$ml_api_url" > /tmp/runtime-config.json

exec nginx -g 'daemon off;'
