# PronounDBLib

Wrapper for https://pronoundb.org in C#

### Features
- Getting pronouns from all 5 supported connections
    - `.GetMinecraftPronounsAsync(minecraft uuid)`
    - `.GetDiscordPronounsAsync(discord id)`    
    - `.GetGithubPronounsAsync(github username)`    
    - `.GetTwitterPronounsAsync(twitter username)` (untested, see testing file for details.)     
    - `.GetTwitchPronounsAsync(Twitch username, twitch client id, twitch client secret)` (I hate twitch api I hate twitch api I hate twitch api I hate twitch api)

- Automatic conversion from github/twitch username to ID

### To-do
- Bulk fetching

