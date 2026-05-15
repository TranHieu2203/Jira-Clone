#!/bin/sh
# LocalStack init hook — chạy 1 lần sau khi LocalStack S3 service ready.
# Tạo bucket `jira-clone` cho cả form_mgmt templates + submission outputs +
# tương lai mọi blob storage khác trong app.
#
# `awslocal` tự load endpoint http://localhost:4566 + dummy AWS creds.
# Reference: https://docs.localstack.cloud/references/init-hooks/

set -e

BUCKET_NAME="jira-clone"

echo "[localstack-init] Creating bucket: $BUCKET_NAME"
awslocal s3 mb "s3://$BUCKET_NAME" || echo "[localstack-init] Bucket $BUCKET_NAME đã tồn tại — skip."

# CORS: cho phép BE/FE call trực tiếp (POC; production tighten origins).
awslocal s3api put-bucket-cors --bucket "$BUCKET_NAME" --cors-configuration '{
  "CORSRules": [
    {
      "AllowedHeaders": ["*"],
      "AllowedMethods": ["GET", "PUT", "POST", "DELETE", "HEAD"],
      "AllowedOrigins": ["*"],
      "ExposeHeaders": ["ETag"]
    }
  ]
}'

echo "[localstack-init] Bucket $BUCKET_NAME ready với CORS config."
awslocal s3 ls
