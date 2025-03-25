#!/usr/bin/env python3

import hashlib
import io
import os
import pathlib
import sys


def sum_file(hasher, file_path):
    assert callable(hasher), 'the hasher must be callable'
    hasher = hasher()
    file_path = pathlib.Path(file_path)

    buffer_size = io.DEFAULT_BUFFER_SIZE
    with file_path.open('rb') as f:
        data = f.read(buffer_size)
        while len(data) > 0:
            hasher.update(data)
            data = f.read(buffer_size)

    return hasher.hexdigest()


def main(args):
    sum_type = args[1]
    if sum_type not in hashlib.algorithms_available:
        raise ValueError('the hash algorithm is not available')
    func = getattr(hashlib, sum_type)
    assert callable(func), 'the sum_type attribute was not callable'

    for arg in args[2:]:
        path = pathlib.Path(arg)
        if path.is_file():
            file = path.resolve(strict=True)
            digest = sum_file(func, path)
            line = f'{digest} *{file.name}'
            print(line, flush=True)


if '__main__' == __name__:
    main(sys.argv)
