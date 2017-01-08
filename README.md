# EEScript
[![Build status](https://ci.appveyor.com/api/projects/status/y0efuvgnonuh7l9w?svg=true)](https://ci.appveyor.com/project/atillabyte/eescript)
[![License](https://img.shields.io/github/license/atillabyte/EEScript.svg?label=License&maxAge=86400)](./LICENSE.txt)

EEScript is a purely functional scripting language that aims to be incredibly easy for non-programmers to pick up and learn.

![Screenshot](https://atil.la/x/img/xFrMMBtsV.png)

### A brief overview of what EEScript is
There are a few different types of triggers, we'll cover the basics first.

Everybody Edits Scripts are created by combining different trigger types, causes, conditions, areas, filters and effects.

You can combine these trigger types to form chunks of script that can be used to automate actions within your worlds.

A simple line of EEScript translates in common terms like this:
> _"If this happens, do this."_

A slightly more complex example would be, 
> _"If this happens, and this condition is true, then do this."_

Whereas the most complicated example would be something like,
> _"If this happens, and this condition is true, then in this area, but only where this other condition is true, do this."_

## Trigger Categories

* _**Causes**_ will be the first line of any block of EEScript script. Without a cause, you can't have an effect. Makes sense, doesn't it?
  Causes can be anything from moving into a certain position, placing a block to changing your smiley.
  All cause headers start with the number 0, and they're the first group of EEScript lines you'll see in the editor.

* _**Conditions**_ if you have any, will always follow directly after the cause. Conditions are statements that need to be true before the effect takes place.
  For instance, you can have a cause be 'Whenever someone moves,' but you only want the effect to take place if their name is Joey, so you add the condition 'and the triggering player's name is {Joey}.'

* _**Areas**_ always come after the cause and conditions and they deal with the area in which the effect will take place.
  Areas can be from anything specifying a position in the world, to every position the player can see.

* _**Filters**_ always come after areas, and they narrow down anything within an area. 
  For example, if your area is 'everywhere around the triggering player' and you only want to place a block where there's a background, you can add a filter to do so.

* _**Effects**_ are the final trigger type, they change something in the world you're in.
  The change can be anything from a new block being placed somewhere, to changing the room name, or giving someone a crown. You can have as many effects after a cause as you'd like.
  Effects should always be the last trigger in the block.
