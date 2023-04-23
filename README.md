프로젝트에 참조중인 오브젝트를 검색하는 Tool입니다.

프로젝트를 진행하다보면 Texture, Material, Script 등등 어디에서 사용되는지 불분명한 자원들이 생겨 납니다.
그런 상황이 프로젝트때마다 빈번하게 발생하여 Tool을 만들었습니다.
해당 Tool을 사용하여, 어디에 사용되고 있는지, 불필요한 자원인지 체크할 수 있습니다.

- 현재 씬에만 국한되어 있지 않고 프로젝트 내의 모든 프리팹을 체크합니다.

- 참조 가능한 타입은 아래와 같습니다.
- Material / Texture2D / AnimatorController / RenderTexture 
- AudioClip  / MonoBehavior / TMP_Font / Font
 
