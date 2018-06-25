#!/bin/bash

curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
sudo mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-xenial-prod xenial main" > /etc/apt/sources.list.d/dotnetdev.list'

sudo apt-get install apt-transport-https
sudo apt-get update
sudo apt-get install dotnet-sdk-2.1.105

sudo apt-get install libleveldb-dev sqlite3 libsqlite3-dev libunwind8-dev

sudo apt-get install unzip

wget -q https://github.com/neo-project/neo-cli/releases/download/v2.7.4/neo-cli-linux-x64.zip

unzip -q neo-cli-linux-x64.zip

cd neo-cli

wget -q https://www.dropbox.com/s/bbn6duy4uq1wt0i/protocol.json -O protocol.json

wget -q https://www.dropbox.com/s/tnicq8k0a7n2q9o/config.json -O config.json

wget -q https://www.dropbox.com/s/bhakb737f9av3uo/wallets.zip

unzip -q wallets.zip

rm -r -f wallets.zip

dotnet neo-cli.dll
