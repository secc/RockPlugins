#!/bin/bash

ls
find ./temp -mindepth 1 -not -name 'readme.MD' -exec rm -rf {} +
cp -r ./playwright-scripts/user-actions ./temp
USERNAME=$(grep -oP 'USERNAME=\K.*' ./playwright-scripts/.env)
PASSWORD=$(grep -oP 'PASSWORD=\K.*' ./playwright-scripts/.env)

if [ -z "$USERNAME" ]; then
  echo "Error: USERNAME not found in ./playwright-scripts/.env file"
  exit 1
fi

if [ -z "$PASSWORD" ]; then
  echo "Error: PASSWORD not found in ./playwright-scripts/.env file"
  exit 1
fi
find ./temp -type f -exec sed -i "s/process.env.USERNAME/'$USERNAME'/g" {} +
find ./temp -type f -exec sed -i "s/process.env.PASSWORD/'$PASSWORD'/g" {} +

artillery run  load-test.yml