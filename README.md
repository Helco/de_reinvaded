# Deutschland Reinvaded

This is (or was) a short experiment to decipher the asset formats of "Invasion Deutschland" a game of Davilex. Likely I will not pursue this much further.
Also most (or all) of the work done here, should also apply to the game "Amsterdoom".

## Current State

There are 010 Editor templates for almost everything regarding `.ACT` files (only missing `TKEvents`).
The work I have done was mostly digging through the old source code of the underlying engine ([Genesis3D](https://github.com/RealityFactory/Genesis3D)) and translating what I understood.
There were some modifications needed to be compatible with "Invasion Deutschland", so don't expect everything to work with other Genesis3D stuff.

There is a small .NET core library and according cli to:

  1. Convert GeBitmaps (such found in `abc.ACT/Body/Bitmaps/*`) to PNG files
  2. Extract VFile archives
