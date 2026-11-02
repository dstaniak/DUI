# Developer ID and DUI notarization

Signing is optional. A regular push to `main` still produces ad hoc packages.
The setup below enables Developer ID signing and Apple notarization for
both macOS architectures. The game continues to be distributed through GitHub/itch.io.

## 1. Create a certificate

You need an active Apple Developer Program membership and Account Holder
permissions to create a Developer ID certificate.

1. Open **Xcode → Settings → Accounts** and add your Apple account.
2. Select the appropriate team, **Manage Certificates**, then **+ → Developer ID Application**.
3. Once created, right-click the certificate and select **Export Certificate**.
   Save the `.p12` file outside the repository and protect it with a password.

The export must include the certificate and its private key. A `.cer` file
alone is not enough. DUI requires a **Developer ID Application** certificate;
Apple Development, Apple Distribution, and Developer ID Installer certificates
serve other purposes.

If you create the certificate through Apple's portal, generate a CSR in
Keychain Access, select Developer ID Application in the portal, upload the CSR,
and install the downloaded `.cer` on the same Mac. Then export the identity as `.p12`.
[Creating a certificate — Apple](https://developer.apple.com/help/account/certificates/create-developer-id-certificates/)
[Managing and exporting in Xcode — Apple](https://developer.apple.com/documentation/xcode/sharing-your-teams-signing-certificates)

## 2. Add GitHub secrets

In the repository, select **Settings → Secrets and variables → Actions → Secrets**.
Add five repository secrets:

| Name | Contents |
|---|---|
| `APPLE_CERTIFICATE_P12_BASE64` | The Base64-encoded `.p12` file |
| `APPLE_CERTIFICATE_PASSWORD` | The password set when exporting `.p12` |
| `APPLE_ID` | The Apple Developer account's email address |
| `APPLE_TEAM_ID` | The Team ID from the developer account's membership information |
| `APPLE_APP_PASSWORD` | A separate app-specific password created for DUI notarization |

To copy the certificate to the clipboard on a Mac:

```sh
base64 -i "/full/path/DeveloperID.p12" | pbcopy
```

Paste the result directly into the GitHub secret. Create the app-specific password at
**account.apple.com → Sign-In and Security → App-Specific Passwords**.
This password is for notarization, separate from the `.p12` export password and
main Apple password. Do not put the certificate, key, or passwords in the
repository or chat. The Team ID alone is not a signing key.
[Certificate secrets — GitHub](https://docs.github.com/en/actions/how-tos/deploy/deploy-to-third-party-platforms/sign-xcode-applications)
[App-specific passwords — Apple](https://support.apple.com/en-us/102654)

## 3. Test and enable releases

1. Run **Actions → Desktop builds → Run workflow** on `main`, enabling
   **Sign and notarize macOS packages** (`sign_macos`).
2. Download both macOS packages. `BUILD.txt` should contain
   `macOS distribution: Developer ID + Apple notarization`.
3. After downloading the ZIP through a browser, test launching from Finder,
   audio, and graphics on both architectures. Successful notarization alone
   does not test whether the game works.
4. In **Settings → Secrets and variables → Actions → Variables**, add the
   repository variable `MACOS_SIGNING_ENABLED` with the value `true`.
   From then on, `vA.B.C.D` tags automatically sign and notarize both Mac packages.

Without this variable, tags keep the existing ad hoc mode. The manual
`sign_macos` option allows a trial before enabling signing for tags.
Signing supports only `main` and release tags; pull requests run tests without
secrets. A missing secret or Apple error stops the signed build,
without publishing ad hoc packages as a replacement.

## What the workflow does

The [setup script](../scripts/setup-macos-signing.sh) imports the identity into
a temporary keychain on an ephemeral GitHub runner. It checks the certificate
type and matching Team ID, then saves a `notarytool` profile.
Secrets are available only in the macOS setup step. The final step removes
the keychain even after a failure; the `.p12` file is removed immediately after setup.
Do not use the setup script on your own Mac or a self-hosted runner.

[release.py](../scripts/release.py) signs all bundled native libraries and
`createdump`, followed by `DUI.app`. The Developer ID signature enables hardened
runtime and an Apple timestamp. The main executable keeps `allow-jit` for .NET.
Apple's service must return `Accepted`; the notarization log goes into the job log.
The script staples the ticket to the app, creates the final ZIP, and checks
the signature, ticket, and Gatekeeper assessment after extraction. Only then
are checksums and artifacts created. Windows and Linux keep their existing packaging.

The wait for Apple has a 20-minute timeout. If it times out, check the submission
ID in the log; the service may still be processing it. The job does not release
a package in that case. After resolving the problem, rerun the workflow.
[Notarization — Apple](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution)
[Signing and JIT in .NET — Microsoft](https://learn.microsoft.com/en-us/dotnet/core/deploying/macos)

## Local signing

On a Mac with the identity installed, display its SHA-1 fingerprint:

```sh
security find-identity -v -p codesigning
xcrun notarytool store-credentials dui-notary
```

The second command interactively prompts for Apple credentials. Then run:

```sh
python3 scripts/release.py package --rid osx-arm64 --version v1.0.0.0 \
  --source artifacts/publish/osx-arm64 \
  --signing-identity "40-CHARACTER-SHA1-FINGERPRINT" --notary-profile dui-notary
```

Substitute the actual fingerprint, version, and architecture. The optional
`--keychain` points to a separate keychain; by default, tools use the user's keychain.
Signing and notarization always go together. Without both options, an ad hoc package is created.
