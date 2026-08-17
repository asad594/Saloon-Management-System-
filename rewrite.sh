git rev-list --reverse HEAD~36..HEAD > /tmp/commits.txt
rm -f /tmp/datemap.txt
i=0
while read commit; do
  hour=$((5 + i/12))
  min=$(( (i%12)*5 ))
  date=$(printf "2026-08-08T%02d:%02d:00+05:00" $hour $min)
  echo "$commit $date" >> /tmp/datemap.txt
  i=$((i+1))
done < /tmp/commits.txt

git filter-branch -f --env-filter '
NEWDATE=$(grep "^$GIT_COMMIT " /tmp/datemap.txt | cut -d" " -f2)
if [ -n "$NEWDATE" ]; then
  export GIT_AUTHOR_DATE="$NEWDATE"
  export GIT_COMMITTER_DATE="$NEWDATE"
fi
' HEAD~36..HEAD
