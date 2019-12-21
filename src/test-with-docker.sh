#!/bin/bash

# Run unit tests in Docker - See Dockerfile
docker build -t libobjectfile-tests . \
    && docker run libobjectfile-tests
