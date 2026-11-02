import contextlib
import io
import json
from pathlib import Path
import subprocess
import sys
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import release


class MacSigningTests(unittest.TestCase):
    def setUp(self):
        self.signing = release.MacSigning("A" * 40, "dui-notary", Path("/tmp/test.keychain-db"))

    def test_signing_requires_identity_and_notary_profile(self):
        for identity, profile in (("-", "profile"), ("", "profile"),
                                  ("A" * 40, ""), ("A" * 40, " ")):
            with self.subTest(identity=identity, profile=profile), self.assertRaises(ValueError):
                release.MacSigning(identity, profile)

    def test_partial_cli_configuration_never_packages_ad_hoc(self):
        arguments = ["release.py", "package", "--rid", "osx-arm64",
                     "--version", "v1.0.0.0", "--source", "unused"]
        for extra in (["--signing-identity", "A" * 40],
                      ["--notary-profile", "dui-notary"], ["--keychain", "test.keychain-db"]):
            with self.subTest(extra=extra), patch.object(sys, "argv", arguments + extra), \
                    patch.object(release, "package") as package, \
                    contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit) as error:
                release.main()
            self.assertEqual(2, error.exception.code)
            package.assert_not_called()

    def test_developer_id_signing_enables_timestamp_and_hardened_runtime(self):
        with patch.object(release, "run") as run:
            release.sign_mac_code(Path("DUI.app"), self.signing, Path("jit.entitlements"))
        command = run.call_args.args
        self.assertIn("--timestamp", command)
        self.assertIn("runtime", command)
        self.assertIn(self.signing.identity, command)
        self.assertIn(str(self.signing.keychain), command)
        self.assertIn(Path("jit.entitlements"), command)
        self.assertNotIn("--deep", command)
        self.assertNotIn("--timestamp=none", command)

    def test_ad_hoc_signing_needs_no_apple_identity_or_timestamp(self):
        with patch.object(release, "run") as run:
            release.sign_mac_code(Path("libcoreclr.dylib"))
        run.assert_called_once_with("codesign", "--force", "--sign", "-",
                                    "--timestamp=none", Path("libcoreclr.dylib"))

    def test_accepted_notarization_staples_then_validates_ticket_and_gatekeeper(self):
        response = json.dumps({"id": "submission", "status": "Accepted"})
        result = subprocess.CompletedProcess([], 0, stdout=response)
        with patch.object(release.subprocess, "run", return_value=result) as submit, \
                patch.object(release, "run") as run, contextlib.redirect_stdout(io.StringIO()):
            release.notarize_mac_app(Path("DUI.app"), self.signing)
        command = submit.call_args.args[0]
        self.assertIn("--wait", command)
        self.assertIn("20m", command)
        self.assertIn("dui-notary", command)
        calls = [call.args for call in run.call_args_list]
        self.assertEqual(("xcrun", "notarytool", "log"), calls[1][:3])
        self.assertEqual(("xcrun", "stapler", "staple", Path("DUI.app")), calls[2])
        self.assertEqual(("xcrun", "stapler", "validate", Path("DUI.app")), calls[3])
        self.assertEqual("spctl", calls[4][0])

    def test_rejection_timeout_and_tool_failure_never_staple(self):
        cases = [("Invalid", 0), ("Invalid", 1), ("In Progress", 0),
                 ("In Progress", 1), ("Accepted", 1)]
        for status, returncode in cases:
            response = json.dumps({"id": "submission", "status": status})
            result = subprocess.CompletedProcess([], returncode, stdout=response)
            with self.subTest(status=status, returncode=returncode), \
                    patch.object(release.subprocess, "run", return_value=result), \
                    patch.object(release, "run") as run, contextlib.redirect_stdout(io.StringIO()), \
                    self.assertRaises((ValueError, subprocess.CalledProcessError)):
                release.notarize_mac_app(Path("DUI.app"), self.signing)
            self.assertFalse(any("stapler" in call.args for call in run.call_args_list))
            if status == "Invalid":
                self.assertTrue(any("log" in call.args for call in run.call_args_list))

    def test_signing_cannot_be_silently_ignored_on_other_platforms(self):
        for rid in ("win-x64", "linux-x64"):
            with self.subTest(rid=rid), self.assertRaises(ValueError):
                release.package(rid, "v1.0.0.0", Path("unused"), Path("unused"), self.signing)


if __name__ == "__main__":
    unittest.main()
