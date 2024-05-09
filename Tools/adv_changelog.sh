#!/bin/sh

special_symbol=:adv_cl:
set -xue

{
i=1
echo Entries:
for hash in $(git log --grep=${special_symbol} --format=format:%H%n | tac)
do
  echo "- author: \"$(git show --format=format:%an -s ${hash})\""
  echo "  changes:"
  git show --format=format:%B ${hash} | grep ${special_symbol} | cut -d: -f3- |
  {
    while read message
    do
      echo "  - message: \"${message}\""
      echo "    type: Add"
    done
  }
  echo "  id: ${i}"
  echo "  time: $(git show --format=format:%aI -s ${hash})"
  i=$(expr ${i} + 1)
done
} >| Resources/Changelog/ChangelogAdv.yml
