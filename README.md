# SellFromTerminal

Sell From Terminal adds a few commands that let you sell scrap directly from your ship.

- `sell all` will sell all the scrap on your ship

- `sell <amount>`, where `amount` is any positive integer, will attempt to sell enough scrap to perfectly match that amount

- `sell quota` will attempt to sell enough scrap to perfectly match the amount needed to fulfill quota

Note: This mod uses an approximation for finding a perfect match, it may not find one even if one exists. This is necessary to keep the game performing well even when processing hundreds of items. If a perfect match isn't found, the received credits will just be slightly over the requested amount.

Config options for:

- Whether or not to sell shotguns and their ammo

- Whether or not to sell gifts

- Whether or not to advance the day and quota when meeting quota from using one of the commands

- How much allowance to grant the algorithm when calculating a perfect match. Ex: Setting it to 5 will make `sell 50` also accept 51, 52, 53, 54 and 55
