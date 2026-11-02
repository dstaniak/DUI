#!/bin/bash
# Run only on a disposable GitHub-hosted macOS runner, never on a developer Mac.
set -euo pipefail

if [[ "${GITHUB_ACTIONS:-}" != "true" || "${RUNNER_OS:-}" != "macOS" ]]; then
  echo "This script requires a GitHub Actions macOS runner." >&2
  exit 1
fi
if [[ "$GITHUB_REF" != "refs/heads/main" && "$GITHUB_REF" != refs/tags/v* ]]; then
  echo "Signing is only allowed on main or a release tag." >&2
  exit 1
fi

: "${APPLE_CERTIFICATE_P12_BASE64:?Missing certificate secret}"
: "${APPLE_CERTIFICATE_PASSWORD:?Missing certificate password secret}"
: "${APPLE_ID:?Missing Apple account secret}"
: "${APPLE_TEAM_ID:?Missing Apple team secret}"
: "${APPLE_APP_PASSWORD:?Missing Apple app-specific password secret}"

umask 077
certificate_path="$RUNNER_TEMP/dui-signing.p12"
keychain_path="$RUNNER_TEMP/dui-signing.keychain-db"
trap 'rm -f "$certificate_path"' EXIT
printf '%s' "$APPLE_CERTIFICATE_P12_BASE64" | base64 --decode > "$certificate_path"
keychain_password=$(openssl rand -base64 32)
echo "::add-mask::$keychain_password"
security create-keychain -p "$keychain_password" "$keychain_path"
security set-keychain-settings -lut 3600 "$keychain_path"
security unlock-keychain -p "$keychain_password" "$keychain_path"
security import "$certificate_path" -P "$APPLE_CERTIFICATE_PASSWORD" \
  -T /usr/bin/codesign -t cert -f pkcs12 -k "$keychain_path" >/dev/null
security set-key-partition-list -S apple-tool:,apple:,codesign: \
  -s -k "$keychain_password" "$keychain_path" >/dev/null

# Select exactly one Developer ID Application identity from this keychain.
# Other certificate types or a different team must not silently sign a build.
identity=$(security find-identity -v -p codesigning "$keychain_path" | python3 -c '
import os, re, sys
identities = re.findall(r"([0-9A-Fa-f]{40}) \"Developer ID Application: [^\"]+ \(([^)]+)\)\"", sys.stdin.read())
matching = [fingerprint for fingerprint, team in identities if team == os.environ["APPLE_TEAM_ID"]]
if len(matching) != 1:
    sys.exit("Expected one valid Developer ID Application identity for APPLE_TEAM_ID.")
print(matching[0])
')

xcrun notarytool store-credentials dui-notary --keychain "$keychain_path" \
  --apple-id "$APPLE_ID" --team-id "$APPLE_TEAM_ID" \
  --password "$APPLE_APP_PASSWORD" >/dev/null

{
  echo "MACOS_SIGNING_IDENTITY=$identity"
  echo "MACOS_NOTARY_PROFILE=dui-notary"
  echo "MACOS_SIGNING_KEYCHAIN=$keychain_path"
} >> "$GITHUB_ENV"
