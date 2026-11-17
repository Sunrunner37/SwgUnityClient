# SWG Unity Client

A brand-new Unity client for **Star Wars Galaxies pre-CU**.

To get started, edit `client.ini`, then open the project in the Unity Editor.

## FAQ

### 1. Why did you use Unity?

Unity is widely accessible to hobbyist developers and has a ton of support and documentation. Since SWG is such an old game, I didn't feel that it absolutely required bleeding-edge code. I'm effectively trading performance for developer accessibility and familiarity.

### 2. Did you use AI? If so, in what capacity?

Yes, this project uses AI extensively. Building a client from scratch requires solving a million little problems. Many are busywork, such as parsing custom file formats or handling hundreds of packet types. It's absolutely possible to do by hand, but there isn't really a reason to.

I've been very intentional about building a good foundation and letting the AI build incremental features. I review each feature and continually refactor to reduce the risk of slop. The AI has spent more time on code review and refactoring than on building features.

### 3. Anything to say about the project development?

Years ago, I built a similar core for a [multiplayer mod for Subnautica](https://github.com/SubnauticaNitrox/Nitrox). I've used a similar pattern in this project.

The published SWGEmu server code has been used as a spec. This project only takes a dependency on the client TREs, nothing else. 

Working with AI isn't perfectly foolproof, but intentionality reduces risk. Spot something dumb? Feel free to drop a PR. 
