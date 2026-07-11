// Generate password PostgreSQL dan keypair JWT langsung di Key Vault tanpa menimpa secret yang sudah ada.
param baseName string
param location string
param vaultName string
param deployIdentityId string

resource generateSecrets 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: 'script-${baseName}-secrets'
  location: location
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${deployIdentityId}': {} }
  }
  properties: {
    azCliVersion: '2.63.0'
    retentionInterval: 'PT1H'
    cleanupPreference: 'OnSuccess'
    timeout: 'PT20M'
    environmentVariables: [
      { name: 'VAULT', value: vaultName }
    ]
    scriptContent: '''
      set -euo pipefail

      # Role Secrets Officer bisa belum terpropagasi saat container mulai — tunggu sampai bisa list.
      for attempt in $(seq 1 10); do
        az keyvault secret list --vault-name "$VAULT" --maxresults 1 >/dev/null 2>&1 && break
        echo "RBAC belum aktif (attempt $attempt); tunggu 20s..."; sleep 20
      done

      # Image azure-cli (alpine) tak membawa openssl; python3+cryptography selalu ada (dependency az).
      if ! az keyvault secret show --vault-name "$VAULT" --name 'pg-admin-password' >/dev/null 2>&1; then
        PG_PASSWORD=$(python3 -c "import secrets; print(secrets.token_urlsafe(24))")
        az keyvault secret set --vault-name "$VAULT" --name 'pg-admin-password' --value "$PG_PASSWORD" >/dev/null
      fi

      if ! az keyvault secret show --vault-name "$VAULT" --name 'jwt-signing-key' >/dev/null 2>&1; then
        python3 - <<'PY'
from cryptography.hazmat.primitives.asymmetric import rsa
from cryptography.hazmat.primitives import serialization

key = rsa.generate_private_key(public_exponent=65537, key_size=2048)
with open('/tmp/jwt.pem', 'wb') as f:
    f.write(key.private_bytes(serialization.Encoding.PEM, serialization.PrivateFormat.PKCS8, serialization.NoEncryption()))
with open('/tmp/jwt-pub.pem', 'wb') as f:
    f.write(key.public_key().public_bytes(serialization.Encoding.PEM, serialization.PublicFormat.SubjectPublicKeyInfo))
PY
        az keyvault secret set --vault-name "$VAULT" --name 'jwt-signing-key' --file /tmp/jwt.pem >/dev/null
        az keyvault secret set --vault-name "$VAULT" --name 'jwt-public-pem' --file /tmp/jwt-pub.pem >/dev/null
        rm -f /tmp/jwt.pem
      else
        az keyvault secret show --vault-name "$VAULT" --name 'jwt-public-pem' --query value -o tsv > /tmp/jwt-pub.pem
      fi

      # Modulus+exponent base64url untuk validate-jwt APIM (material publik, aman jadi output).
      modulus_b64url=$(python3 - <<'PY'
import base64
from cryptography.hazmat.primitives import serialization

with open('/tmp/jwt-pub.pem', 'rb') as f:
    pub = serialization.load_pem_public_key(f.read())
n = pub.public_numbers().n
raw = n.to_bytes((n.bit_length() + 7) // 8, 'big')
print(base64.urlsafe_b64encode(raw).decode().rstrip('='))
PY
      )
      public_pem=$(cat /tmp/jwt-pub.pem)

      jq -n --arg pem "$public_pem" --arg mod "$modulus_b64url" \
        '{jwtPublicPem: $pem, jwtModulus: $mod, jwtExponent: "AQAB"}' > "$AZ_SCRIPTS_OUTPUT_PATH"
    '''
  }
}

output jwtPublicPem string = generateSecrets.properties.outputs.jwtPublicPem
output jwtModulus string = generateSecrets.properties.outputs.jwtModulus
output jwtExponent string = generateSecrets.properties.outputs.jwtExponent
