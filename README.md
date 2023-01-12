# Portal Framework

## Description

A Portal project made in Unity.
Can be used and modified freely according to the MIT license.

## Features

Portal:
* Seamless character movement and camera rendering through portals
* Can transforms position, rotation and scale
* Portals can target themselve as a destionation
* Seamless directional lighting through portals
* Raycast through portals
	
Basic character controller:
* Can be rotated in any direction
* Quake strafe jump
* Counter Strike air control / surf
* Grab object

## GIFs

<details>
  <summary>Gif 1</summary>
  
  ![gif-1](https://github.com/FredMarquer/PortalFramework/blob/main/Gifs/portal-1.gif)
</details>

<details>
  <summary>Gif 2</summary>
  
  ![gif-2](https://github.com/FredMarquer/PortalFramework/blob/main/Gifs/portal-2.gif)
</details>

<details>
  <summary>Gif 3</summary>
  
  ![gif-3](https://github.com/FredMarquer/PortalFramework/blob/main/Gifs/portal-3.gif)
</details>

<details>
  <summary>Gif 4</summary>
  
  ![gif-4](https://github.com/FredMarquer/PortalFramework/blob/main/Gifs/portal-4.gif)
</details>

## Requirements

* Unity 2021.3.13f1 (didn't test with other versions of Unity)

## Not supported (yet ?)

* Shadows through portals
* Sound through portals
* Animation synchronization between teleportable object and clone
* Moving portals (need to recompute portal corners and bounds)

## Known issues

* Anti aliasing generate artifact on the portal edges
* Renderer of TeleportableObject/Clone in a portal is not clipped and can be seen on the backside of a portal
* Collider of TeleportableObject/Clone in a portal exists and can collide with other stuff on the backside of the portal
* Seamless lighting not perfect for some material/shader (Unity's Standard shader for exemple)
* Some time this error shows up: Screen position out of view frustum
* Can't held an object through multiple portals
* TeleportableObject don't handle being in multiple portal triggers simultaneously (if 2 portals are close enough)
* The camera near clip can clip with portal mesh when scaled up
