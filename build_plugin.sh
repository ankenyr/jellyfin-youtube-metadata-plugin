#!/bin/bash
#
# Copyright (c) 2020 - Odd Strabo <oddstr13@openshell.no>
#
#
# The Unlicense
# =============
#
# This is free and unencumbered software released into the public domain.
#
# Anyone is free to copy, modify, publish, use, compile, sell, or
# distribute this software, either in source code form or as a compiled
# binary, for any purpose, commercial or non-commercial, and by any
# means.
#
# In jurisdictions that recognize copyright laws, the author or authors
# of this software dedicate any and all copyright interest in the
# software to the public domain. We make this dedication for the benefit
# of the public at large and to the detriment of our heirs and
# successors. We intend this dedication to be an overt act of
# relinquishment in perpetuity of all present and future rights to this
# software under copyright law.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
# EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
# MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
# IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
# OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
# ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
# OTHER DEALINGS IN THE SOFTWARE.
#
# For more information, please refer to <http://unlicense.org/>
#

MY=$(dirname $(realpath -s "${0}"))
JPRM="jprm"

PLUGIN=${1:-${PLUGIN:-.}}

ARTIFACT_DIR=${ARTIFACT_DIR:-"${MY}/artifacts"}
mkdir -p "${ARTIFACT_DIR}"

meta_version=$(grep -Po '^ *version: * "*\K[^"$]+' "${PLUGIN}/build.yaml")

find "${PLUGIN}" -name project.assets.json -exec rm -v '{}' ';'

zipfile=$($JPRM --verbosity=debug plugin build "${PLUGIN}" --output="${ARTIFACT_DIR}" --version="${meta_version}") && {
    $JPRM --verbosity=debug repo add --url=https://raw.githubusercontent.com/ankenyr/jellyfin-plugin-repo/master/ "/workspaces/jellyfin-plugin-repo/" "${zipfile}"
}
exit $?
