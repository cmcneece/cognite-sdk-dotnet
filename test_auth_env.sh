#!/bin/bash
# Load credentials from .env file and fetch tokens

set -e

# Source .env file
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
else
    echo "Error: .env file not found"
    exit 1
fi

# Build the token URL and scope
TOKEN_URL="https://login.microsoftonline.com/${TENANT_ID}/oauth2/v2.0/token"
SCOPE="https://${CDF_CLUSTER}.cognitedata.com/.default"

echo "Fetching token for project: ${CDF_PROJECT} on ${CDF_CLUSTER}..."

# Fetch token (using python for JSON parsing since jq may not be available)
RESPONSE=$(curl -sX POST \
    --fail \
    -F client_id="${CLIENT_ID}" \
    -F client_secret="${CLIENT_SECRET}" \
    -F grant_type='client_credentials' \
    -F scope="${SCOPE}" \
    "${TOKEN_URL}" 2>&1)

if [[ $? -ne 0 ]]; then
    echo "Error: Token request failed"
    echo "$RESPONSE"
    exit 1
fi

# Parse token using python (more universally available than jq)
TOKEN=$(echo "$RESPONSE" | python3 -c "import sys, json; print(json.load(sys.stdin).get('access_token', ''))" 2>/dev/null)

if [[ -z "$TOKEN" ]]; then
    echo "Error: Could not get token. Check your credentials."
    echo "Response: $RESPONSE"
    exit 1
fi

echo "✓ Token acquired successfully"

# Export for tests
export TEST_TOKEN_WRITE=$TOKEN
export TEST_TOKEN_READ=$TOKEN
export TEST_PROJECT_WRITE=$CDF_PROJECT
export TEST_PROJECT_READ=$CDF_PROJECT
export TEST_HOST_WRITE="https://${CDF_CLUSTER}.cognitedata.com"
export TEST_HOST_READ="https://${CDF_CLUSTER}.cognitedata.com"

# Also export original variables for other scripts
export TEST_TENANT_ID_WRITE=$TENANT_ID
export TEST_CLIENT_ID_WRITE=$CLIENT_ID
export TEST_CLIENT_SECRET_WRITE=$CLIENT_SECRET
export TEST_TENANT_ID_READ=$TENANT_ID
export TEST_CLIENT_ID_READ=$CLIENT_ID
export TEST_CLIENT_SECRET_READ=$CLIENT_SECRET

echo "Environment configured:"
echo "  Project: $CDF_PROJECT"
echo "  Cluster: $CDF_CLUSTER"
echo "  Host: $TEST_HOST_WRITE"
