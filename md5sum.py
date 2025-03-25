#!/usr/bin/env python3

import hashlib
import io
import os
import pathlib
import sys


def md5(file_path):
    hasher = hashlib.md5()
    file_path = pathlib.Path(file_path)

    buffer_size = io.DEFAULT_BUFFER_SIZE
    with file_path.open('rb') as f:
        data = f.read(buffer_size)
        while len(data) > 0:
            hasher.update(data)
            data = f.read(buffer_size)

    return hasher.hexdigest()


def main(args):
    for arg in args[1:]:
        path = pathlib.Path(arg)
        if path.is_file():
            file = path.resolve(strict=True)
            digest = md5(path)
            line = f'{digest} *{file.name}'
            print(line, flush=True)


if '__main__' == __name__:
	main(sys.argv)
