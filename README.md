# GameKit
Pre-built commonly needed gameplay elements, with examples. Developed for Fish-Networking.
https://github.com/FirstGearGames/FishNet

All features are server authoritative with client prediction.

![Simple Preview](https://github.com/FirstGearGames/GameKit/blob/main/FirstGearGames/GameKit/Repository/simple_preview.png?raw=true)


Features:
  
    General:
    * Efficient action serialization.
    * Expand core types easily.
    * Built-in API to easily handle situational changes on most features.
    * Basic new/old Unity input key checking.

    Utility:
    * FloatingResourceCanvas to easily show dragging icons.
    * TooltipCanvas to show tooltips anywhere.
    * Splitting canvas to split anything with quantities.
    * Offline object pooling, such as for particles.
    * Several general extensions and performance API such as RingBuffers and more...
    
    
    Resources:
    * Categories (eg: Equipped, Food, Scraps).
    * Stack limits.
    * Maximum limits.
    * Display information.
    * Obtainable hidden resources such as zone tokens, currency, more.
    * More...
    
    Inventory:
    * Add, remove bags at runtime.
    * Add, remove resources at runtime.
    * Move bagged resources for custom layouts.
    * Synchronized custom layouts.
    * Customizable bags including: size, category, description, more...
    * State change callbacks.
    * Cross inventory communication (eg: between character inventory to bank inventory).
    * Hidden inventory items such as: quest tokens, currency, more...
    * UI including: stacking items, tooltips, stack splitting, moving, displaying bags and resources, moving resources, search bar, more...
    
    Crafting:
    * Recipes.
    * Custom crafting times.
    * Consecutive craft multiplier.
    * Automatic refresh on craftable recipes when inventory changes.
    * UI including: recipes list, recipe preview, craft start and stop buttons, crafting progress bar.

    Chat:
    * Message limits.
    * Toggable bad word filters.
    * Private chat (whispers/tells).
    * Team chat.
    * Global chat.
    * UI including: message color indicators (for team, private, global), auto private chat completion, click to private chat, more...

    XP/Leveling:
    * Easily modifable levleing template; can be used for skill levels, XP, more...
    * Example leveling template for XP.

    Providers (Placeholder/future prep):
    * Providers can be anything non-client in the game that supplies items, quests, goals, more...

    Questing:
    This feature is still in development.    
      Working:
      * Quest condition template.
      * Gather, interact, travel condition. These can be used for virtually any kind of quest, including talk to.
      * Custom quest creation which includes: trackable, title, description, conditions, rewards, more...
      
      Presumable Working (untested):
      * Add, remove quests at runtime.
      * Check quest complete conditions.
      * Quest only droppable resources including Providers which can drop, quantity, rarity, more...
      
      Incomplete:
      * Rewards: a placeholder has been made but not implemented.
      * Inventory updates to automatically check completion. Quester(or own impleementation) should use Inventory API to review changes and see if they progress quests.
    
    
