#!/bin/bash
# Run integration tests against CDF using credentials from .env file

set -e

echo "=== Setting up environment ==="
source test_auth_env.sh

echo ""
echo "=== Running integration tests ==="
echo "Target: ${CDF_CLUSTER}/${CDF_PROJECT}"
echo ""

# Run only the DataModels tests (most relevant for our extensions)
# To run all tests, remove the --filter option
dotnet test CogniteSdk/test/csharp/CogniteSdk.Test.CSharp.csproj \
    --filter "FullyQualifiedName~DataModels" \
    --logger "console;verbosity=normal"

echo ""
echo "=== Integration tests complete ==="
