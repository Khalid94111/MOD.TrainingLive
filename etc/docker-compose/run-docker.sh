#!/bin/bash

if [[ ! -d certs ]]
then
    mkdir certs
    cd certs/
    if [[ ! -f localhost.pfx ]]
    then
        dotnet dev-certs https -v -ep localhost.pfx -p 20fc70d9-1865-4a9a-b15f-3b5395acce47 -t
    fi
    cd ../
fi

docker-compose up -d
