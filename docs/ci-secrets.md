# CI Secrets Reference

These GitHub repository secrets are required for the CI/CD pipelines to work.

## Unity License (for GameCI)

| Secret | Where to get it | Used by |
|--------|----------------|---------|
| `UNITY_LICENSE` | Contents of your `.ulf` file (see below) | test.yml, deploy.yml |
| `UNITY_EMAIL` | Your Unity account email | test.yml, deploy.yml |
| `UNITY_PASSWORD` | Your Unity account password | test.yml, deploy.yml |

### Getting your Unity license file

1. Install Unity Hub and sign in
2. Activate a license (Personal is fine)
3. Find the `.ulf` file:
   - **Linux**: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
   - **macOS**: `/Library/Application Support/Unity/Unity_lic.ulf`
   - **Windows**: `C:\ProgramData\Unity\Unity_lic.ulf`
4. Copy the **entire file contents** into the `UNITY_LICENSE` secret

## App Store Connect API (for Fastlane)

| Secret | Where to get it | Used by |
|--------|----------------|---------|
| `ASC_KEY_ID` | App Store Connect > Users and Access > Integrations > Team Keys | deploy.yml |
| `ASC_ISSUER_ID` | Same page, shown at the top | deploy.yml |
| `ASC_PRIVATE_KEY` | Contents of the `.p8` file (downloaded when creating the key) | deploy.yml |

### Creating an API key

1. Go to [App Store Connect](https://appstoreconnect.apple.com) > Users and Access > Integrations > Team Keys
2. Click "+" to generate a new key
3. Name: "CI Upload Key", Role: "Developer" (sufficient for builds + TestFlight)
4. Download the `.p8` file immediately (you can only download it once)
5. Store the file contents as `ASC_PRIVATE_KEY`

If you already have an API key from CAWCAW, you can reuse the same `ASC_KEY_ID`, `ASC_ISSUER_ID`, and `ASC_PRIVATE_KEY`.

## Fastlane Match (for code signing)

| Secret | Where to get it | Used by |
|--------|----------------|---------|
| `FASTLANE_MATCH_GIT_URL` | SSH URL of your match certificates repo (e.g., `git@github.com:you/certs.git`) | deploy.yml |
| `FASTLANE_MATCH_PASSWORD` | Encryption password for the match repo | deploy.yml |
| `FASTLANE_MATCH_DEPLOY_KEY` | SSH private key with read access to the match repo | deploy.yml |

### Setting up the deploy key

1. Generate an SSH key pair: `ssh-keygen -t ed25519 -f match_deploy_key -N ""`
2. Add the **public key** (`match_deploy_key.pub`) as a deploy key on your match certificates repo (Settings > Deploy keys)
3. Add the **private key** (`match_deploy_key`) contents as the `FASTLANE_MATCH_DEPLOY_KEY` secret

If reusing CAWCAW's match repo, you can reuse the same deploy key, password, and git URL.

## Summary

If you're reusing CAWCAW's existing setup, you likely only need to add:
- `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` (new — Unity-specific)

And reuse from CAWCAW:
- `ASC_KEY_ID`, `ASC_ISSUER_ID`, `ASC_PRIVATE_KEY`
- `FASTLANE_MATCH_GIT_URL`, `FASTLANE_MATCH_PASSWORD`, `FASTLANE_MATCH_DEPLOY_KEY`
