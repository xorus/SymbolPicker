# Symbol Picker

***THIS IS A VERY EARLY TEST PROJECT, IT MIGHT ACTUALLY RE-CONSTRUCT DALAMUD AND BRING IT DOWN UPON OUR VERY EARTH.***

Testing repo : https://xorus.dev/xiv

Or grab a build from the GitHub actions (if they work).

Use `control`+`.` to open the quick character picker.

Use the `/charmap` command to open the character list.

## why

I once did `win`+`.` and it opened an emoji picker on Windows, and I was like WOAAA so I made it so I could go WOAAA in
ffxiv too.

## todo

- the search input is not focused the first time you open it
- unproper unload causes crashes?
- left/right to navigate in search results (actually implemented but broken :( )
- fix favourite not saving properly
- auto-paste on click/enter
- customize open shortcut
- open main UI button in mini-search
- ordering for favourites (drag and drop?)
- better UI
- build characters file in CI

## build

It's a bit of a mess, but I didn't want to include a 7mb lib in the build just for unicode character descriptions... If
you want to change the supported 'standard' characters, you need to build and run the `BuildChars` project, as it will
generate the `characters.json` data file that will then be embeded into the main project.

I don't have enough msbuild knowledge to do this "the right way" so there it is.
