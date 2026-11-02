#!/bin/bash

################################################## variables ##################################################

################################################## functions ##################################################

update_dotnet_packages() {
  find . -name "*.csproj" | sort | while read csproj; do
    echo "Processing project: $csproj"
    dotnet list "$csproj" package --outdated | grep -E '>' | awk '{print $2, $4}' |
    while read name version; do
      echo "Updating package: $name to project: $csproj"
      dotnet add "$csproj" package "$name"
    done
  done
  # dotnet list package --outdated
  dotnet tool update --all # Update all tools
}

solution_initilize(){
  dotnet tool restore
}

run_test() {
  project=$1
  rm -rf $project/TestResults
  dotnet test $project --settings $project/coverlet.runsettings --collect:"XPlat Code Coverage"
  dotnet reportgenerator -reports:"$project/TestResults/*/coverage.cobertura.xml" -targetdir:"$project/TestResults/CoverageReport" -reporttypes:Html
  echo "$PWD/$project/TestResults/CoverageReport/index.html"

  firefox "$PWD/$project/TestResults/CoverageReport/index.html" &
  # google-chrome "$PWD/$project/TestResults/CoverageReport/index.html" &
  # /opt/microsoft/msedge/msedge "$PWD/$project/TestResults/CoverageReport/index.html" &

  # "/c/Program Files/Mozilla Firefox/firefox.exe" "$PWD/$project/TestResults/CoverageReport/index.html" &
  # "/c/Program Files/Google/Chrome/Application/chrome.exe" "$PWD/$project/TestResults/CoverageReport/index.html" &
}

################################################## execute ##################################################
clear
# update_dotnet_packages
# solution_initilize

run_test Base.Tests
