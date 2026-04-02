# 프로젝트 관리 및 개발 규칙 (Project Rules)

**중요: 현재 핵심 시스템 구현 단계이므로 `MainScene`은 비워둡니다. 각자 본인의 이름으로 된 Scene을 생성하여 테스트를 진행해 주시기 바랍니다.**
- *예시: `JaeinScene`*
- 모든 씬 저장은 `00.Scenes` 폴더 내에서 관리합니다.

## 1. 폴더 구조 (Folder Structure)
프로젝트 자산의 체계적인 관리를 위해 아래의 번호 순서와 구조를 유지합니다.

* **00.Scenes**: 모든 씬 파일 (.unity)
* **01.Scripts**: 모든 C# 스크립트
    * **Editor**: 유니티 에디터 확장 및 커스텀 인스펙터 스크립트 (빌드 시 제외됨)
* **02.Prefabs**: 재사용 가능한 프리팹 자산
* **03.Art**: 3D 모델, 텍스처, 메테리얼 등 아트 자산
* **04.UI**: UI 관련 이미지, 스프라이트 및 UI 프리팹
* **05.Audio**: 사운드 이펙트(SFX) 및 배경음(BGM)
* **06.VFX**: 이펙트 및 파티클 시스템
* **07.Data**: ScriptableObject, JSON, CSV 등 데이터 파일
* **08.Shaders**: 커스텀 셰이더 및 셰이더 그래프 파일
* **09.Plugins**: 외부 라이브러리 및 에셋 스토어 플러그인
* **99.Test**: 개인 테스트용 자산 및 임시 파일 (이름_Test 형식을 권장)
* **Resources**: `Resources.Load`를 통해 런타임 로드가 필요한 특수 자산

---

## 2. Git 브랜치 전략 및 규칙
GitFlow를 기반으로 하며, 모든 작업은 피처 단위로 분리하여 진행합니다.

### 브랜치 구조
- **main**: 최종 배포 및 빌드용 브랜치. 검증된 코드만 병합합니다.
- **dev**: 개발 통합 브랜치. 모든 피처가 모이는 중심 브랜치입니다.
- **feature/**: 단위 기능 구현 브랜치.

### 피처 브랜치 명명 규칙
- **형식**: `feature/기능명`
- **작성 요령**: 영문 **소문자**만 사용하며, 공백은 **하이픈(-)**으로 대체합니다.
- *예시: `feature/player-movement`, `feature/enemy-ai`, `feature/ui-inventory`*

---

## 3. 머지(Merge) 및 충돌 관리 규칙
안정적인 코드 통합을 위해 아래 순서를 엄격히 준수합니다.

1.  **사전 확인**: `dev`에 합치기 전, 원격 브랜치의 최신 내용을 반드시 확인합니다.
2.  **선 병합 (Pre-merge)**: `dev` 브랜치를 자신의 `feature` 브랜치로 먼저 머지하여 로컬에서 충돌 여부를 확인합니다.
3.  **충돌 해결**: 충돌 발생 시, 해당 기능을 작업한 본인이 직접 해결합니다.
4.  **무결성 유지**: **컴파일 에러가 있는 상태로는 절대 `dev`로 머지하지 않습니다.**
5.  **최종 통합**: 로컬 테스트 완료 후 문제가 없을 시 `dev`로 머지합니다.

---

## 4. 코드 컨벤션 (C# Naming Convention)
일관된 코드 스타일 유지를 위해 다음 네이밍 규칙을 따릅니다.

* **PascalCase**: 클래스(Class), 메서드(Method), 프로퍼티(Property)
* **_camelCase**: `private` 필드(Field). 접두어 언더바(_) 사용
* **camelCase**: 지역 변수(Local Variable), 파라미터(Parameter)

### 코드 예시
```csharp
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed; // private 필드
    private bool _isDead;

    public float MoveSpeed => _moveSpeed; // 프로퍼티

    public void Move(float moveInput) // 메서드 및 파라미터
    {
        float targetVelocity = moveInput * _moveSpeed; // 지역 변수
        // 로직 구현...
    }
}
```

---

## 5. 작업 흐름 요약
1.  `dev`에서 `feature/기능-이름` 브랜치 생성
2.  `00.Scenes` 내 개인용 Scene에서 기능 구현 및 테스트 (`MainScene` 수정 금지)
3.  구현 완료 후 `dev`의 최신 내용을 내 브랜치로 가져와 충돌 해결 및 컴파일 확인
4.  문제가 없으면 `dev`로 머지
5.  최종 검증 완료 시 `main`에서 빌드 진행 및 버전 체크 완료 후 배포
