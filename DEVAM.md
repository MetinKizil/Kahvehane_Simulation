# KAN BAĞI — Proje Devir Dosyası

> Bu dosya, yeni bir sohbet oturumunda projeye kaldığı yerden devam edebilmek için hazırlandı.
> **Kodların tamamı diskte mevcut** — okumak için: `D:\KAHVE DENEME\KAHVEHANEDENEME\Assets\_Project\Scripts\`

---

## 1. ROL VE GÖREV

Sen kıdemli bir Unity (C#) oyun geliştiricisi ve sistem mimarısın. "Kan Bağı" adında, 3D URP tabanlı, hikaye ve işletme odaklı bir kahvehane/mahalle simülasyonu geliştiriyoruz.

## 2. OYUNUN RUHU VE HİKAYESİ

> **Bu metin oyunun tonunu belirler** (2026-10-10'da netleşti). Hikaye girişi, diyaloglar, menü metinleri ve bildirim dili buna göre yazılır: sıradan bir esnaflık oyunu değil, **intikam destanı.**

Babamız ve amcamız, zamanında bu mahallenin temelini beraber atan, sırt sırta vermiş iki efsane ortaktı. Ancak babamızın şüpheli ölümüyle her şey tepetaklak oldu. Kan bağımız olan öz amcamız, babamızın taziye çayı bile soğumadan bize acımasızca tekmeyi bastı. Yetmezmiş gibi, kendi yediği pis bir haltı bizim üstümüze yıktı ve gençliğimizi demir parmaklıklar ardına gömdü. Yıllar sonra dört duvar arasından çıktığımızda, koca mirastan elimizde kalan tek şey; babamızın ilk göz ağrısı olan, köşede çürümeye yüz tutmuş izbe bir kıraathaneydi.

Ama amcamızın hesaba katmadığı bir şey vardı: Biz o delikte sadece volta atmadık, sokağın kurallarını da öğrendik. Şimdi amacımız o yıkık dökük çay ocağını küllerinden doğurmak. PC'den stok yönetip çay demlerken bir yandan da mahallede para ve "İtibar" kazanacağız. Kazandığımız güçle diğer dükkanları tek tek satın alıp sokağın mutlak hakimi olacak, bizi sırtımızdan vuran amcamızın tahtını başına yıkacağız. İşler büyüdükçe dükkanın arka odasına gizli bir kumarhane açacak, mafyayla aynı masaya oturup polisle köşe kapmaca oynayacağız. Bu sıradan bir esnaflık serüveni değil; bu, kanla yazılmış bir intikam ve mahalleyi geri alma destanı.

**Hikayenin oynanışa yansıması:**
- ✅ Yeni oyunda bu hikaye 5 sayfalık **giriş ekranıyla** anlatılıyor (Adım 10a-1, metin `GirisHikayesi.asset`'te)
- ✅ Oyun içi olaylara intikam tonunda **"mahalle fısıltıları"** eşlik ediyor (Adım 10a-2a, metin `MahalleFisiltilari.asset`'te)

**Henüz yapılmadı, yol haritasında:**
- Amca bir **NPC/antagonist** olarak mahallede var olmalı; mülkleri satın almak onun gücünü kırmak anlamına gelmeli
- Mahalleyi ele geçirmek bir **bitiş koşulu** sunmalı (amcanın tahtını yıkmak)
- "İtibar" sadece bir sayı değil, mahallenin seni kabul etmesi

## 2.5 NİHAİ HEDEF — CO-OP

Oyun **eşli (co-op) oynanacak şekilde** çıkacak. Örnek senaryo: biri tezgahta çay dağıtıp bulaşık yıkarken diğeri arka odada kumarhaneyi yönetiyor veya mafyayla ilgileniyor.

**Karar (2026-10-10): netcode EN SONA (Faz 5), ama mimari bugünden korunur.** Gerekçe ve kurallar bölüm 10'da.

## 3. YAZILIM ANAYASASI (KESİN KURALLAR)

1. **SOLID ve Temiz Kod** — Spagetti kod kesinlikle yasak. Sınıflar sadece kendi işini yapar.
2. **Observer Pattern** — Sistemler birbirine `Action` / `UnityEvent` ile bağlanır. `Update` içinde başka script'i sürekli kontrol etmek (polling) yasak.
3. **Veri Yönetimi** — Ürünler, fiyatlar, envanter verileri kesinlikle **ScriptableObject** (`ItemData`) ile tutulur.
4. **UI Yönetimi** — Arayüz için **TextMeshPro**. UI kodları oyun mantığından (Manager'lar vb.) tamamen ayrı tutulur.
5. **CO-OP UYUMU** (2026-10-10 eklendi) — Nihai hedef eşli oynanış olduğu için:
   - **Oyuncuya özel durum singleton olamaz.** İki oyuncunun ayrı eli, ayrı kilidi, ayrı bakışı olacak.
   - **Dünya durumu singleton kalabilir** (Economy, Inventory, Day, Upgrade, Property, Mafia, Police) — co-op'ta bunlar server-authoritative tek kopya olur.
   - Durum değişiklikleri **her zaman manager metodundan** geçer, UI veya istasyon doğrudan alan yazmaz. Bu metotlar ileride RPC sınırı olacak.
   - Dükkan mantığı "tek oyuncu var" varsayamaz. **9b'den beri:** etkileşim `IInteractable.GetPrompt/Interact(PlayerContext player)` ile gelir; oyuncunun eli/kilidi `player.Hands` / `player.Lock`'tan alınır. `PlayerHands`/`PlayerLock` singleton değil. `Camera.main` sadece her istemcinin kendi ekranında yaptığı işte (Billboard) kabul.

## 4. ÇALIŞMA AKIŞI (kullanıcının açık talebi)

1. Her adımda ne yapılacağı **önce anlatılır, onay alınır**.
2. Onaydan sonra kod yazılır.
3. Unity Editor içinde yapılması gerekenler (sahne kurulumu, Inspector bağlantıları, NavMesh bake vb.) **adım adım kullanıcıya tarif edilir** — kullanıcı kendisi yapar.
4. Kullanıcı "yaptım / çalışıyor" deyince sonraki adıma geçilir.

**İzinsiz kod yazılmaz.** Adımlar küçük tutulur (kullanıcı büyük adımları ikiye bölmeyi tercih ediyor).

**Computer use kuralı (9a'dan itibaren):** Yapılacak her iş önce kullanıcıya sorulur. Onay gelirse Unity Editor adımlarını (derleme kontrolü, menü komutları, sahne kaydetme, kısa Play testi) Claude **computer use ile kendisi yapar**; kullanıcıya sadece sonucu kontrol etmek kalır. Unity `full` yetkiyle geliyor. Adımlar mümkün olduğunca **menü tıklamasıyla** yapılabilecek şekilde tasarlanır (builder/Editor komutları). Kumar/servis gibi oynanış testleri kullanıcıda kalır.

---

## 5. PROJE ORTAMI

| | |
|---|---|
| Proje yolu | `D:\KAHVE DENEME\KAHVEHANEDENEME` |
| Unity | `6000.3.10f1` (Unity 6.3) |
| Render Pipeline | URP 17.3.0 |
| Input | **New Input System 1.18.0** — `Assets/InputSystem_Actions.inputactions`, project-wide actions olarak atanmış |
| Navigation | AI Navigation 2.0.10 (NavMeshSurface ile bake edildi) |
| UI | uGUI 2.0.0 + TextMeshPro |
| Aktif sahne | `Assets/Scenes/SampleScene.unity` (dukkan sahnesi; henuz yeniden adlandirilmadi) |

### Klasör yapısı
```
Assets/_Project/
├── Prefabs/           (CayBardagi, Musteri, Kumarbaz, ...)
├── ScriptableObjects/ (Cay, ..., MusteriProfilleri/)
├── Scenes/            (Dukkan)
├── Materials/
└── Scripts/
    ├── Core/          ReputationManager, UpgradeManager, UpgradeUnlocker, DayCycleManager,
                       DailyStatsManager, ISaveable, SaveManager, PropertyManager, BalanceLogger
    ├── Customers/     Customer, CustomerSpawner, CustomerModifiers, CustomerPatienceBoost,
                       TableGameBoost
    ├── Data/          ItemData, ItemDatabase, ShopCatalog, UpgradeData, UpgradeCatalog,
                       PropertyData, PropertyCatalog, StoryData, WhisperData, TextPreset,
                       CustomerProfile
    ├── Delivery/      DeliveryManager
    ├── Editor/        RowPrefabBuilder, ReportPanelBuilder, SaveMenuItems, SignPrefabBuilder,
                       GamblingSignBuilder, MafiaPrefabBuilder, PolicePrefabBuilder,
                       BalancePresetApplier, GamblerPrefabBuilder, NavMeshChecker,
                       StoryPanelBuilder, WhisperLineBuilder, TextPresetApplier,
                       CustomerProfileLinker, TableGamesBuilder, BeverageStationBuilder,
                       TrayBuilder, FurnitureSetupBuilder, PlacementModeBuilder
    ├── Illegal/       GamblingTable, SuspicionManager, MafiaManager, MafiaCollector,
                       PoliceManager, PoliceOfficer, Gambler, GamblerSpawner
    ├── Neighborhood/  PropertyBuilding, PropertyVisualSwitch
    ├── Economy/       EconomyManager, PurchaseManager
    ├── Interaction/   IInteractable, HoldableItem, PlayerInteractor
    ├── Inventory/     InventoryManager
    ├── Furniture/     PlacedFurniture, FurnitureManager, NavMeshRuntimeBaker, UpgradeVisual,
                       UpgradeFurnitureGrant, FurniturePlacer
    ├── Items/         TeaGlass, DeliveryBox, Tray
    ├── Player/        PlayerController, PlayerHands, PlayerLock, PlayerContext (düz sınıf, bileşen değil)
    ├── Shop/          TimedStation, TeaStove, Sink, GlassShelf, Table, TableManager,
                       ComputerStation, StorageRack, MenuManager, StationSpeedBoost, TraySpot
    └── UI/            MoneyUI, ReputationUI, StockUI, HeldItemUI, InteractionPromptUI, ProgressBarUI,
                       OrderBubbleUI, Billboard, ComputerUI, CatalogEntryUI, DeliveryNotificationUI,
                       TabGroupUI, UpgradeListUI, UpgradeEntryUI, ClockUI, DailyReportUI,
                       PropertyListUI, PropertyEntryUI, PropertySignUI, GamblingSignUI, MafiaSignUI,
                       PoliceNotificationUI, StoryIntroUI, WhisperUI, PlacementHintUI
```

### Kod konvansiyonları (mevcut koda uyulmalı)
- Namespace'ler: `KanBagi.Core`, `KanBagi.Customers`, `KanBagi.Data`, `KanBagi.Economy`, `KanBagi.Interaction`, `KanBagi.Inventory`, `KanBagi.Items`, `KanBagi.Player`, `KanBagi.Shop`, `KanBagi.UI`
- Yorumlar **Türkçe ama şapkasız/noktasız** (ASCII) — `cay`, `musteri`, `siparis` gibi. Kod içinde Türkçe karakter kullanılmıyor.
- Manager'lar `public static X Instance { get; private set; }` singleton deseni kullanıyor (`EconomyManager`, `ReputationManager`, `TableManager`...). **Oyuncuya özel bileşenler singleton olamaz** (9b).
- Inspector alanları mümkün olduğunca **opsiyonel**: boşsa kod otomatik bulur (`GetComponentInChildren`, `FindFirstObjectByType`, ofset fallback'leri). Bulamazsa **Türkçe, adresi net `Debug.LogError`** basar ve component'i `enabled = false` yapar.
- Durum makineleri `Update` yerine **coroutine** ile yazılıyor.

---

## 6. ŞU ANA KADAR YAPILANLAR

### Adım 1 — Çekirdek mimari ✅
- `ItemData` (ScriptableObject): itemName, description, icon, itemType (`Beverage`/`Consumable`/`TableItem`), sellPrice, purchaseCost, prepareDuration. Menü: `Create > Kan Bagi > Item Data`
- `EconomyManager`: `CurrentMoney`, `AddMoney(int)`, `TrySpendMoney(int) : bool`, `event Action<int> OnMoneyChanged`. Başlangıç 250 TL.
- `ReputationManager`: `CurrentReputation`, `AddReputation(int)`, `event Action<int> OnReputationChanged`. Başlangıç 0. **Faz 4'te mafya/polis buna abone olacak.**
- `MoneyUI`: sadece `OnMoneyChanged` dinler.

### Adım 2 — Oyuncu ve etkileşim ✅
- `PlayerController`: FPS yürüme + bakış (CharacterController + Input System "Player" map'i, `Move`/`Look`). Kamerayı ve input asset'ini otomatik bulur.
- `IInteractable`: `string InteractionPrompt { get; }` + `void Interact()` → **9b'de `GetPrompt(PlayerContext)` + `Interact(PlayerContext)` oldu**
- `PlayerInteractor`: kameradan raycast (2.5m), odak değişince `event Action<IInteractable> OnFocusChanged`, `Interact` action'ının `started`'ında `Interact()` çağırır. Etkileşim sonrası ipucunu tazeler; nesne yok edildiyse odağı null'lar. Oyuncunun kendi collider'ını yok sayar. **Tek raycast + `hit.collider.TryGetComponent`** kullanır → `IInteractable` script'i collider ile **aynı GameObject'te** olmalı.
- `InteractionPromptUI`: Canvas üzerinde, `[E] ...` ipucu.
- ~~`MoneyTestInteractable`~~: geçici test kutusuydu, **Adım 4b'de silindi**.

### Adım 3a — Taşıma sistemi ve bardak ✅
- `HoldableItem` (`IInteractable`): `DisplayName`, `IsHeld`, `AttachTo(Transform)`, `Detach(pos, rot, restorePhysics)`, `SetPickupEnabled(bool)`. Ele alınınca collider/rigidbody kapanır.
- `PlayerHands` (~~singleton~~ — 9b'de çıktı): tek slot. `HeldItem`, `IsEmpty`, `TryHold(HoldableItem) : bool`, `ReleaseHeldItem() : HoldableItem`, `TryDropAt(...)`, `event Action<HoldableItem> OnHeldItemChanged`. Tutma noktasını (`HoldAnchor`) kameranın altında **otomatik oluşturur**.
- `TeaGlass`: `enum GlassState { Empty, Full, Dirty }`, `State`, `Content (ItemData)`, `TryFill(ItemData) : bool`, `MarkDirty()`, `Clean()`, `GetStateLabel()`, `event Action<GlassState> OnStateChanged`. Görselleri (`liquidVisual`, `dirtyVisual`) duruma göre açıp kapatır.
- `GlassShelf` (`IInteractable`): temiz bardak stoğunun **tek sahibi**. `CleanGlassCount`, `AddCleanGlass()`, `event Action<int> OnCleanGlassCountChanged`. Bardak verir / boş bardağı geri alır.
- `HeldItemUI`: elde ne olduğunu gösterir, bardağın durum değişimlerine de abone olur.

### Adım 3b — Çay ocağı ve lavabo ✅ (bulaşık döngüsü kapandı)
- `TimedStation` (abstract, `IInteractable`): süreli işlerin ortak temeli. `IsBusy`, `WorkLabel`, `StartWork(float, Action)`, `event Action<TimedStation, float> OnProgressChanged`, `event Action<TimedStation> OnWorkFinished`.
- `TeaStove : TimedStation`: elde boş bardak varsa alır, `beverageData.PrepareDuration` kadar doldurur, sonra "Bardağı Al". Doldururken bardak alınamaz.
- `Sink : TimedStation`: kirli **veya dolu** ("Dök ve Yıka") bardağı alır, yıkar, bardağı yok edip `GlassShelf.AddCleanGlass()` ile **stoğa iade eder**. Rafı otomatik bulur.
- `ProgressBarUI`: istasyonları otomatik bulur, ilerleme çubuğunu doldurur.

### Adım 4a — Masa ve müşteri ✅
- `Table`: `Occupant`, `IsFree`, `SeatPosition`, `GlassPosition`, `GlassRotation`, `TryReserve(Customer)`, `Release()`. Ofsetler fallback'li, Scene'de gizmo çizer (mavi = koltuk, sarı = bardak).
- `TableManager` (singleton, `[DefaultExecutionOrder(-100)]`): `Register/Unregister`, `TryReserveFreeTable(Customer, out Table)`, `HasFreeTable()`.
- `Customer` (NavMeshAgent + coroutine durum makinesi): `enum CustomerState { Arriving, WalkingToTable, WaitingForOrder, Leaving }`, `State`, `IsWaitingForOrder`, `Initialize(Vector3 exit)`, event'ler: `OnStateChanged`, `OnPatienceChanged(float)`, `OnLeft(Customer)`. Sabır 25 sn; masaya oturunca agent kapanır, kalkınca `NavMesh.SamplePosition` + `Warp` ile geri açılır.
- `CustomerSpawner`: aralıklı üretim, `maxActiveCustomers` limiti, boş masa yoksa göndermez.
- `OrderBubbleUI` + `Billboard`: müşterinin başında "Çay" balonu; sabır azaldıkça yazı beyazdan kırmızıya döner.

### Adım 4b — Servis ve ekonomi ✅ (FAZ 1 KAPANDI)
- `CustomerState`'e **`Drinking`** eklendi: `Arriving → WalkingToTable → WaitingForOrder → Drinking → Leaving`.
- `Customer` yeni API'si:
  - `ItemData Order` (Inspector: `desiredOrder` — prefab'da `Cay` atalı)
  - `bool CanAcceptOrder(TeaGlass)` — sipariş bekliyor + bardak `Full` + içerik siparişle uyuşuyor. `desiredOrder` boşsa herhangi bir `Beverage` kabul edilir. **Masanın ipucu metni ve teslim anı bu tek kuralı paylaşır.**
  - `bool TryServe(TeaGlass)` — kabul ederse satışı işler ve `Drinking`'e geçer.
  - Inspector: `drinkDuration` (6 sn), `reputationOnServed` (+1), `reputationOnAngryLeave` (1 → eksi olarak işlenir).
- `WaitForOrderRoutine` artık `State == WaitingForOrder` koşuluyla döner → servis edilince **sabır döngüsünden kendiliğinden çıkar** (polling yok).
- `CompleteSale(ItemData)`: `EconomyManager.AddMoney(SellPrice)` + `ReputationManager.AddReputation(+1)`. **Para/itibar mantığının tek yeri burası** — ileride `SalesManager`'a taşınacaksa değişecek tek metot.
- `LosePatience()`: servis edilmeden gidince `AddReputation(-1)`.
- `DrinkRoutine`: içerken bardağın `HoldableItem.SetPickupEnabled(false)` (oyuncu çalamaz), süre bitince `MarkDirty()` + pickup tekrar açılır → **kirli bardak masada kalır**.
- `Table` artık `IInteractable`:
  - Bardağı `GlassPosition/GlassRotation`'a `Detach(restorePhysics: false)` ile bırakır (kinematik kalır, yuvarlanmaz), ardından `SetPickupEnabled(true)` ile collider'ı geri açar.
  - `bool HasGlass` — `placedGlassItem != null && !placedGlassItem.IsHeld`; oyuncu bardağı alınca veya lavaboda yok edilince yuva **kendiliğinden** boşalır.
  - İpuçları: "Siparisi Ver" / "Musteri bunu istemiyor" / "Once masadaki bardagi kaldir" / "Musteri oturuyor" / "Bos masa".
  - Bilinçli kural: **müşteri yokken masaya çay bırakılamaz.**
- Kirli bardağı almak için Table'a gerek yok — bardağın kendi `HoldableItem`'ı zaten `IInteractable`.
- `OrderBubbleUI`: sipariş adı artık `customer.Order.ItemName`'den okunur (ItemData yoksa `orderMessage` yedeği).
- `ReputationUI`: `MoneyUI`'ın kardeşi, sadece `OnReputationChanged` dinler.

### Adım 5a — Stok çekirdeği ✅ (Faz 2 başladı)
- `InventoryManager` (singleton, `KanBagi.Inventory`): sarf malzemesi stoğunun **tek sahibi**.
  - `int GetCount(ItemData)`, `bool HasStock(ItemData, int = 1)`, `bool TryConsume(ItemData, int = 1)`, `void Add(ItemData, int = 1)`
  - `event Action<ItemData, int> OnStockChanged` — ileride yönetim PC'si de buna abone olacak.
  - Başlangıç stoğu Inspector'dan: `List<StockEntry>` (nested `[Serializable]` sınıf, `Item` + `Amount`). Aynı malzeme iki kez yazılırsa toplanır.
  - `Dictionary<ItemData, int>` — key olarak ScriptableObject referansı kullanılıyor.
- Yeni ScriptableObject: **`CayYapragi`** (Consumable, SellPrice 0, PurchaseCost 2).
- `TeaStove` güncellemesi:
  - Yeni alanlar `requiredConsumable` + `consumeAmount`. **Boş bırakılırsa ocak eskisi gibi bedava çalışır** (geriye dönük güvenli).
  - Malzeme **doldurma başlamadan** düşülür (`TryConsumeIngredient`) — bardak ocakta unutulsa bile yaprak yandı sayılır.
  - Stok yoksa ipucu `"Cay Yapragi bitti"` olur, `Interact()` hiçbir şey yapmaz, **bardak oyuncunun elinde kalır**.
  - Stok uyarısı el boşken de görünür (oyuncu boşuna bardak taşımasın).
  - `Start()`'ta InventoryManager yoksa Türkçe LogError (Awake'te bakılmıyor — manager'ın Awake'i sonra çalışmış olabilir).
- `StockUI`: sadece `OnStockChanged` dinler, `trackedItems` listesindeki malzemeleri `StringBuilder` ile satır satır yazar.

### Adım 5b — Yönetim PC'si ✅ (anında teslimat)
- `ShopCatalog` (ScriptableObject, `Create > Kan Bagi > Shop Catalog`): satın alınabilir malzeme listesi.
  - `CatalogEntry`: `Item` + `UnitsPerPack`; **`PackPrice => item.PurchaseCost * UnitsPerPack`** — fiyat koda gömülmez.
  - `IsValid` — eksik doldurulmuş satırlar UI'da gösterilmez.
  - Asset: `MalzemeKatalogu` (CayYapragi × 10 = 20 TL).
- `PurchaseManager` (singleton, `KanBagi.Economy`): **satın almanın tek yeri.**
  - `bool CanAfford(entry)` (buton aktifliği için), `bool TryBuy(entry)` → `TrySpendMoney` + `InventoryManager.Add`
  - `event Action<ItemData, int> OnPurchased`
  - **5c'de değişecek tek metot `TryBuy`** — malzeme depoya değil kuryeye gidecek.
- `PlayerLock` (~~singleton~~ — 9b'de çıktı, Player objesinde): `SetControlEnabled(bool)` → `PlayerController` + `PlayerInteractor` aç/kapa, `event Action<bool> OnControlEnabledChanged`. `PlayerController` zaten `OnEnable/OnDisable`'da imleci yönettiği için **imleç bedavaya doğru çalışıyor.** Faz 4'te kumar masası/polis diyaloğu da bunu kullanacak.
- `ComputerStation` (`IInteractable`): ekranı açar/kapar, `event Action<bool> OnScreenToggled`. ESC için Input System'deki **`UI/Cancel`** action'ına ekran açıkken abone olur (asset'te yoksa sessizce atlar, Kapat butonu kalır).
- `ComputerUI`: **Canvas üzerinde durur** (panelin üzerinde değil — panel kapalıyken `Start()` çalışmaz!). Katalogdan satırları `entryPrefab`'tan kodla üretir; ekran açıkken para/stok event'lerine abone olur, kapanınca aboneliği bırakır.
- `CatalogEntryUI`: tek satır — ad, "10 adet - 20 TL (Depo: 30)", Satın Al butonu. Para yetmiyorsa buton `interactable = false`.
- `PlayerInteractor.OnDisable` küçük ek: kontrol kilitlenince ekranda **asılı kalan `[E]` ipucu temizlenir**.

### Adım 5c — Kurye teslimatı ✅ (Supermarket Simulator döngüsü)
- `DeliveryManager` (singleton, `KanBagi.Delivery`): siparişi coroutine ile bekletir, süre dolunca teslimat alanına koli düşürür.
  - `bool IsReady` (boxPrefab var mı), `bool QueueOrder(ItemData, int)`, `int PendingOrders`
  - `event Action<ItemData, int> OnOrderQueued` / `OnDelivered`
  - Inspector: `deliveryDelay` (test 8 sn), `dropHeight` 0.6, `scatterRadius` 0.35 (koliler üst üste binmesin), `deliveryPoint`
  - `OnDrawGizmosSelected` → teslimat alanı yeşil daire
- `PurchaseManager.TryBuy`: artık `InventoryManager.Add` YOK. **Para düşmeden önce `DeliveryManager.IsReady` kontrol edilir** (para boşa gitmesin), sonra `QueueOrder`.
- `DeliveryBox` (`RequireComponent(HoldableItem)`): `Content`, `Amount`, `IsEmpty`, `Fill(item, count)`, `GetLabel()`.
  - `Fill` içinde `HoldableItem.SetDisplayName(...)` çağrılır → **HeldItemUI'a dokunmadan** "Elinde: Cay Yapragi Kolisi (10)" yazar.
- `HoldableItem.SetDisplayName(string)` eklendi.
- `StorageRack` (`IInteractable`): elde dolu koli varsa `InventoryManager.Add(Content, Amount)` yapıp koliyi yok eder. İpucu: `"Koliyi Bosalt (10 Cay Yapragi)"`.
- `DeliveryNotificationUI`: sadece `OnOrderQueued`/`OnDelivered` dinler, birkaç saniye yazıp kaybolur (`messageText.enabled` ile — objeyi kapatmıyor ki coroutine ölmesin).

### Adım 5d — Upgrade sistemi + PC arayüzü yenilendi ✅
- `UpgradeData` (SO): `UpgradeName`, `Description`, `Cost`, `IsValid`. **Sadece veridir, ne yaptığını bilmez.**
- `UpgradeCatalog` (SO): PC'de listelenecek upgrade'ler. Asset: `GelistirmeKatalogu`.
- `UpgradeManager` (singleton, `KanBagi.Core`): `IsPurchased`, `CanAfford`, `TryPurchase`, `event Action<UpgradeData> OnUpgradePurchased`. Satın alınanlar `HashSet<UpgradeData>`.
- `UpgradeUnlocker` (sahne bileşeni): `requiredUpgrade` + `targets[]`. **SO ↔ sahne köprüsü** (ScriptableObject sahne objesi tutamaz).
  - ⚠️ **Hedefin kendisinde/altında DURMAMALI** — hedef kapalıyken kendisi de kapanır ve event'i duymaz. `Managers`'ta durur (aynı objeye birden fazla eklenebilir).
- `TabGroupUI`: sekme butonu + sayfa çiftleri. Dinleyiciler `Awake`'te, sekme seçimi `OnEnable`'da (panel her açıldığında varsayılan sekmeye döner).
  - ⚠️ Sekme butonlarının **Transition = None** olmalı, yoksa Button'un kendi renk geçişi aktif sekme rengini ezer.
- `UpgradeListUI` + `UpgradeEntryUI`: `ComputerUI`/`CatalogEntryUI`'ın kardeşleri. Alınmış upgrade'de buton `Alindi` olup pasifleşir.
- **PC paneli sıfırdan kuruldu — hiç anchor ayarı yok, her şey Layout Group ile.** Panel `Vertical Layout Group`, sayfalar `Flexible Height 1` (pasif sayfa layout'tan otomatik düşer).
- `RowPrefabBuilder` (**Editor script**): `Kan Bagi > Satir Prefablarini Olustur` menüsü `UrunSatiri` ve `UpgradeSatiri` prefab'larını **kod ile üretir** — ölçüler, fontlar ve script bağlantıları dahil.
  - Satır tasarımı değişecekse dosyanın üstündeki sabitleri değiştirip menüye tekrar tıkla.
  - ⚠️ Yeniden üretim bileşen kimliklerini değiştirir → `ComputerUI > Entry Prefab` ve `UpgradeListUI > Entry Prefab` alanları **yeniden bağlanmalı**.
- İlk upgrade: **Yeni Masa (100 TL)** → `Masa3` açılır. `Table.OnEnable` zaten `TableManager`'a kaydolduğu için **ekstra kod yok**.

### Adım 5e — Kahve, oralet ve menü çeşitliliği ✅ (FAZ 2 KAPANDI)
- `MenuManager` (singleton, `KanBagi.Shop`): dükkanın satabildiği içeceklerin **tek sahibi**.
  - `MenuEntry { Beverage, RequiredUpgrade }` — gerekli upgrade boşsa baştan menüde, doluysa alınınca girer
  - `AvailableBeverages`, `HasAnyBeverage`, `GetRandomBeverage()`, `IsOnMenu()`, `event Action OnMenuChanged`
  - `Start`'ta kurulur ve `UpgradeManager.OnUpgradePurchased`'a abone olur
- `Customer.ResolveOrder()`: `desiredOrder` **boşsa** menüden rastgele seçer. Inspector alanı override olarak kalır.
  - ⚠️ `Musteri` prefab'ında `Desired Order` **boş** olmalı, yoksa herkes aynı şeyi ister
  - Menü kilitli içecekleri içermediği için **müşteri yalnızca yapabildiğimiz şeyi ister**
- `ShopCatalog.CatalogEntry.requiredUpgrade` (opsiyonel): makinen yokken malzemesi listede görünmez → boşa para gitmez. `CatalogEntryUI` satırı `SetActive(false)` yapar, `ComputerUI` artık `OnUpgradePurchased`'a da abone.
- `ProgressBarUI`: `FindObjectsInactive.Include` — kilitli istasyonlar sonradan açılınca çubuk çalışır.
- **`TeaStove` tek satır değişmedi**: kahve makinesi ve oralet semaveri aynı script, farklı ItemData. (İsmi ileride `BeverageStation` olabilir.)
- Yeni upgrade'ler: **Kahve Makinesi 200 TL**, **Oralet Semaveri 100 TL** — her biri hem istasyonu açar hem içeceği menüye sokar.

### Adım 5f — Upgrade etkileri (stat iyileştirmeleri) ✅
- `TimedStation.DurationMultiplier` (varsayılan 1) + `SetDurationMultiplier(float)` — `StartWork` süreyi bununla çarpar. **`TeaStove` ve `Sink` tek satır değişmedi.**
- `StationSpeedBoost` (`KanBagi.Shop`): `requiredUpgrade` + `targetStations[]` + `durationMultiplier`. Pasif (kilitli) istasyonlar da atanabilir.
- `CustomerModifiers` (singleton): `PatienceMultiplier`, `MultiplyPatience(float)`, `event OnModifiersChanged`. Çarpanlar **çarpılarak birikir** → iki upgrade üst üste etki eder.
- `CustomerPatienceBoost` (`KanBagi.Customers`): upgrade alınınca çarpanı büyütür. `isApplied` bayrağı ile **iki kez uygulanmaz**.
- `Customer.GetEffectivePatience()` — sabır artık `patienceDuration × PatienceMultiplier`.
- Yeni upgrade'ler: **Sanayi Tipi Ocak 250 TL** (süre ×0.6), **Okey Takimi 150 TL** (masalara okey açar **+** sabır ×1.5).
- **Okey Takimi tek upgrade'e iki dinleyici**: `UpgradeUnlocker` objeleri açar, `CustomerPatienceBoost` sabrı artırır. Observer Pattern'in neden seçildiğinin örneği.
- ⚠️ Masa üstü dekor objelerinin **Box Collider'ı kaldırılmalı** — yoksa masaya bakınca ışını kesip servis ipucunu engeller.

### Adım 6a — Gün döngüsü ✅
- `DayCycleManager` (singleton, `KanBagi.Core`): gün **00:00 → 24:00 kesintisiz** akar; `openHour`/`closeHour` sadece dükkanın açık olduğu penceredir.
  - `CurrentDay`, `TimeOfDay` (0-24 ondalıklı), `DayProgress`, `IsShopOpen`, **`AcceptsNewCustomers`**, `GetClockLabel()`
  - `event`: `OnDayStarted`, `OnShopOpened`, `OnShopClosed`, `OnDayEnded`, `OnTimeChanged`
  - Inspector: `dayDurationMinutes` 6, **`nightTimeScale` 4** (gece hızlı akar, ölü bekleme olmaz — 1 yaparsan kapanır), `lastCallHours` 1
  - **Müşteri sistemini hiç tanımaz** — bağımlılık tek yönlü.
- `CustomerSpawner`: artık `List<Customer>` tutuyor. `ActiveCustomers`, `event OnCustomerSpawned` (6b için).
  - `CanSpawn()` → `AcceptsNewCustomers` (kapanışa 1 saat kala yeni müşteri yok)
  - `OnShopClosed` → içerideki **herkese `LeaveNow()`** (liste kopyası üzerinde dönülür)
- `Customer.LeaveNow()`: coroutine'leri durdurur, servis edilmediyse `LosePatience()`, `FinishDrink()` ile **yarım kalan bardağı kirli ve alınabilir bırakır** (yoksa oyuncunun asla temizleyemeyeceği kilitli bardak kalırdı).
- `Customer.WasServed` / `PaidAmount` — 6b'nin günlük özeti bunları okuyacak, yeni event gerekmedi.
- `ClockUI`: `Gun 3 - 14:30`, `(SON MUSTERI)` ve `(KAPALI)` etiketleri. Metin değişmediyse yeniden yazmaz (çöp üretmez).

### Adım 6b — Günlük özet paneli ✅
- `EconomyManager.OnMoneySpent` — `TrySpendMoney` **tüm harcamaların tek geçiş noktası** olduğu için malzeme + upgrade gideri tek event'ten yakalanıyor, hiçbir satın alma koduna dokunulmadı.
- `DailyStatsManager` (singleton, `KanBagi.Core`): `CustomersServed`, `CustomersLost`, `Revenue`, `Expenses`, `NetProfit`, `ReputationAtDayStart`, `ReputationChange`.
  - Müşterileri `CustomerSpawner.OnCustomerSpawned` → `customer.OnLeft` zinciriyle sayar; 6a'daki `WasServed`/`PaidAmount` sayesinde **yeni event gerekmedi**.
  - `OnDayStarted`'da sıfırlanır.
- `DayCycleManager`: `RequestNextDay()` + `IsWaitingForNextDay` + `waitForNextDayRequest`. **Gün numarası bekleme bitene kadar artmaz** → panel biten günü gösterir.
- `DailyReportUI`: `OnDayEnded`'a abone, paneli açar, `PlayerLock` ile oyuncuyu kilitler, PC ekranı açıksa kapatır. Net kâr yeşil/kırmızı, itibar `7  (+2)` biçiminde.
  - **Kök hep aktif, görseli yok**; açılıp kapanan `Icerik` (ekran karartması) çocuğu. Kökte görsel olsaydı ekranı sürekli kapatırdı.
  - `NormalizeLayout()`: prefab yanlış ebeveyne bırakılırsa panel köşede minik kalıyordu — script açılışta kendini Canvas'ın doğrudan çocuğu yapıp `SetAsLastSibling()` ile en öne alıyor ve rect'i geriyor.
- `ReportPanelBuilder` (**Editor script**): `Kan Bagi > Gunluk Ozet Panelini Olustur` → paneli kod ile üretir, iç bağlantılar hazır gelir. Kullanıcının tek işi prefab'ı Canvas'a sürüklemek.

### Adım 6c — Kayıt sistemi ✅
- `ISaveable`: `SaveKey`, `string CaptureState()`, `void RestoreState(string)`. **Durum JSON metni olarak taşınır** — `object` yerine string kullanıldı, çünkü `JsonUtility` polimorfizmi ve Dictionary'yi desteklemiyor. Her sistem kendi `JsonUtility` çevrimini yapar.
- `SaveManager` (singleton, **`[DefaultExecutionOrder(100)]`**): sahnedeki tüm `ISaveable`'ları bulur (`FindObjectsInactive.Include`), `Application.persistentDataPath/kanbagi_save.json` dosyasına yazar.
  - `Save()`, `Load()`, `HasSave`, `DeleteSave()`, `OnSaved`, `OnLoaded`
  - **Execution order 100 kritik**: tüm sistemlerin `Start`'ı bittikten sonra yükler, böylece kilit açan/etki uygulayan dinleyiciler hazır olur.
- `ISaveable` uygulayanlar: `EconomyManager` (para), `ReputationManager` (itibar), `InventoryManager` (stok), `UpgradeManager` (upgrade'ler), `DayCycleManager` (gün numarası).
- **ScriptableObject'ler asset adıyla kaydedilir**, yüklerken `ItemDatabase` / `UpgradeCatalog` üzerinden eşlenir.
  - ⚠️ Yeni `ItemData` eklediğinde **`UrunVeritabani`'na da eklemelisin**, yoksa kayıttan geri gelmez.
- ⚠️ **En kritik nokta:** `UpgradeManager.RestoreState` yükleme sonunda **`OnUpgradePurchased`'ı yeniden duyurur**. Yoksa kilitler kapalı kalır, menü büyümez, hız/sabır etkileri uygulanmaz.
- Otomatik kayıt **`OnDayStarted`**'a bağlı, `OnDayEnded`'a değil.
  - Sebep: gün sayacı oyuncu "Yeni Gun" dedikten **sonra** artıyor; gün sonunda kaydetmek diske hep bir önceki günü yazıyordu (test sırasında yakalanan bug).
  - Açılışta boşuna kayıt oluşmaz: 1. günün `OnDayStarted`'ı `DayCycleManager.Start` içinde, SaveManager abone olmadan önce tetiklenir.
- Saat kaydedilmez, **gün baştan (00:00) başlar**.
- PC ekranında **Kaydet** butonu + `Kaydedildi` bildirimi (`ComputerUI`).
- `SaveMenuItems` (Editor): `Kan Bagi > Kaydi Sil`, `Kan Bagi > Kayit Klasorunu Ac`.
- `SaveManager.Load` teşhis logu basar: `[SaveManager] Yuklendi: economy, reputation, ... | Kayitta YOK: ...`

---

## 6.5 FAZ 3 — MAHALLE VE EMLAK

### Adım 7a — Emlak çekirdeği ✅ (itibar ilk kez işlevsel)
- `PropertyData` (SO): `PropertyName`, `Description`, `PurchasePrice`, `DailyIncome`, **`RequiredReputation`**.
- `PropertyCatalog` (SO): liste + `Find(assetName)` (kayıt eşlemesi için).
- `PropertyManager` (singleton, `ISaveable`, `KanBagi.Core`):
  - `IsOwned`, `MeetsReputation`, `CanPurchase`, `TryPurchase`, `TotalDailyIncome`, `OwnedCount`, `event OnPropertyPurchased`
  - `OnDayStarted`'da kirayı `EconomyManager.AddMoney` ile kasaya yazar → **aldığın gün değil, ertesi gün yatar**
  - `LastRentPaid` — o günün kirası. **Event yerine değer** tutuluyor ki "önce kim dinledi" yarışı olmasın.
- `DailyStatsManager.RentIncome` → `PropertyManager.LastRentPaid`'i okur. `NetProfit = Revenue + RentIncome - Expenses`.
- PC'de **3. sekme `EMLAK`** — `TabGroupUI` genişletildi, `PropertyListUI` + `PropertyEntryUI`.
  - İtibar yetmiyorsa butonda fiyat yerine **`Itibar 35 gerekli`** yazar (oyuncu neden alamadığını görür).
- `RowPrefabBuilder` artık `EmlakSatiri` prefab'ını da üretiyor (butonu daha geniş: itibar şartı yazısı sığsın).
- **Test sırasında yakalanan 2 bug:**
  1. `PropertyManager.RestoreState` katalog boşken `owned.Clear()` yapıyordu → veri siliniyordu. Artık katalog yoksa **hiçbir şeye dokunmuyor**.
  2. **`LastRentPaid` kaydedilmiyordu.** Açılışta gün 1 başlarken mülkler henüz yüklenmemiş olduğu için kira 0 hesaplanıyor; bu değer geri yüklenmezse devam edilen günün özetinde kira 0 görünüyordu. Artık kaydedilip geri yükleniyor (kira ikinci kez **ödenmez**, sadece rapora yazılır).

### Adım 7b — Fiziksel mahalle ✅
- `Sokak` (Plane, z = -5 … -15) mevcut `Zemin`in devamı — kapı yok, yürüyerek çıkılıyor (bilinçli tercih, kapı sonra eklenecek).
- Üç bina (Cube, scale 3): `BakkalBinasi`, `TerziBinasi`, `KirathaneBinasi` — her birinde `PropertyBuilding`.
- `PropertyBuilding` (`IInteractable`, `KanBagi.Neighborhood`): mülkü **yerinde** satın alma.
  - İpuçları: `Kosedeki Bakkal - 800 TL` / `Itibar 10 gerekli` / `800 TL gerekli` / `Senin dukkanin - Gunluk 60 TL`
  - PC ekranıyla **aynı `PropertyManager.TryPurchase`**'ı çağırır → tek doğruluk kaynağı, iki arayüz.
  - ⚠️ Collider ile **aynı objede** olmalı (PlayerInteractor tek ışın atıp `hit.collider` üzerinde arar).
- `PropertySignUI`: binanın **duvarına yapışık** SATILIK tabelası. Billboard YOK (sabit durur). Üstünde mülk adı, fiyat, günlük gelir, gerekli itibar. **Satın alınınca tabela tamamen kaybolur.** Kayıttan yüklenmiş mülkün tabelası hiç görünmez.
- `PropertyVisualSwitch`: opsiyonel görsel değişimi (`unownedObjects` / `ownedObjects`).
- `SignPrefabBuilder` (**Editor**) iki menü sunar:
  - `Kan Bagi > Emlak Tabelasi Prefabini Olustur` → levha + iki satır 3D yazı, collider yok
  - **`Kan Bagi > Tabelalari Binalara Yerlestir`** → sahnedeki her binanın **Renderer.bounds'undan** ön duvarı hesaplayıp tabelayı oraya yapıştırır, binanın çocuğu yapar, **parent ölçeğini ters çevirerek** yazının büyümesini engeller, `Building` alanını bağlar, eski tabelayı siler.
  - ⚠️ **Elle koordinat girmeye çalışmayın** — tabela konumu bu komutla hesaplanıyor. Yükseklik `AttachSign` içindeki `bounds.size.y * 0.55f` ile ayarlanır.

---

## 6.6 FAZ 4 — İLLEGAL İŞLER

### Adım 8a — Gizli kumarhane ✅
- `UpgradeData.requiredReputation` (0 = şart yok) + `UpgradeManager.MeetsReputation`. `UpgradeEntryUI` fiyat yerine **`Itibar 50 gerekli`** yazar; `UpgradeListUI` artık `OnReputationChanged`'a da abone (itibar artınca kendiliğinden açılır).
- `GamblingRoundResult` (readonly struct): `Won`, `Stake`, `Payout`, `Net`.
- `GamblingTable` (`IInteractable`, `KanBagi.Illegal`): **sadece dükkan kapalıyken** (`IsOpen => !DayCycleManager.IsShopOpen`).
  - Inspector: `betAmount` 100, `winChance` 0.45, `payoutMultiplier` 2, `suspicionPerRound` 3
  - `NightNet` her gece (`OnShopClosed`) sıfırlanır, `event OnRoundPlayed`
  - İpuçları: `Kumar Oyna (100 TL)` / `Gunduz kumar oynanmaz` / `100 TL gerekli`
- `SuspicionManager` (singleton, `ISaveable`): `CurrentSuspicion`, `SuspicionRatio`, `Add/Reduce`, gün başında `dailyDecay` kadar düşer. **8c'de polis bunu okuyacak.**
- `GamblingSignUI`: masanın üstünde 3D tabela — `BAHIS 100 TL` / `KAZANDIN +100 TL` / `Gece: -200 TL`. Gündüz `GUNDUZ KAPALI`.
- `GamblingSignBuilder` (**Editor**): `Kan Bagi > Kumar Tabelasini Olustur ve Yerlestir` — masanın ölçülerinden hesaplayıp üstüne koyar. **`SignYawDegrees` = 180** (kapı tarafına bakar; tezgahın arkasına dolanmak gerekmesin).
- `GizliKumarhaneUpgrade` (1500 TL + **itibar 50**) → `UpgradeUnlocker` masayı açar.
- **Muhasebe tuzağı çözüldü:** bahis `TrySpendMoney`'den geçtiği için `Expenses`'a yazılıyordu; `DailyStatsManager.HandleGamblingRound` bahsi giderden **geri çıkarıp** `GamblingNet`'e yazıyor → çift sayım yok. `NetProfit = Revenue + RentIncome + GamblingNet - Expenses`.
- **Yakalanan soft-lock:** `GunlukOzetPaneli` sahneden silinince `OnDayEnded`'ı dinleyen kalmıyor, `DayCycleManager` "Yeni Gun" bekleyip **oyunu sessizce kilitliyordu** (otomatik kayıt da durmuş görünüyordu). Artık `OnDayEnded == null` ise uyarı basıp günü kendiliğinden devrediyor.

### Adım 8b — Mafya ve haraç ✅
- `MafiaManager` (singleton, `ISaveable`, `KanBagi.Illegal`):
  - **Baraj:** `requiredReputation` 30 **VE** `requiredMoney` 2000 — gün başında kontrol edilir, bir kez tetiklenir
  - **Zamanlama:** baraj aşılınca hemen gelmez → `firstVisitDelayDays` 2 sonra, ayrıca `earliestVisitDay` 3'ten önce asla. Sonraki ziyaretler `visitIntervalDays` 3 günde bir.
  - `PayDemand()` → `TrySpendMoney` + tahsildar yok olur. **Ödenmezse ertesi gün başında reddedilmiş sayılır**: itibar −5, şüphe +10, `refusalCount++` → sonraki haraç `baseDemand × 1.5^refusalCount`
  - **Ayrı "reddet" tuşu yok** — görmezden gelmek reddetmektir (bilinçli tasarım)
  - `event OnDemandArrived / OnDemandPaid / OnDemandRefused`, `PaidToday`
- `MafiaCollector` (`IInteractable`): haraç aktifken `MafiaManager` tarafından `spawnPoint`'te üretilir, ödenince/reddedilince yok edilir. Kayıttan yüklenince **hâlâ ödenmemişse yeniden belirir**.
- `MafiaSignUI` + `MafiaPrefabBuilder` (**Editor**): `Kan Bagi > Tahsildar Prefabini Olustur` → kapsül gövde + üstünde `HARAC` / `500 TL` tabelası, hepsi bağlı.
  - ⚠️ **Tahsildar tabelasında `Billboard` VAR** (insan, etrafından dolaşılır) — duvara asılı emlak tabelalarında **YOK** (binaya sabit). Billboard varken levha yazının **arkasında** (+Z) olmalı, sabit tabelada **önünde** (−Z). Karıştırılırsa yazı görünmez.
- Günlük özete **"Harac"** satırı. Haraç da `TrySpendMoney`'den geçtiği için `Expenses`'tan düşülüp kendi satırına yazılır (kumar ile aynı çift-sayım çözümü).
  - `NetProfit = Revenue + RentIncome + GamblingNet - Expenses - ProtectionPaid`
- **Test sırasında yakalanan bug:** mafya barajı aştığın **gün** kapıya dayanıyordu (oyun daha oynanmadan). `firstVisitDelayDays` + `earliestVisitDay` ile çözüldü.

### Adım 8c — Polis baskını ✅ (şüphenin ilk gerçek sonucu)
- **Gizleme mekaniği** `GamblingTable`'a eklendi: `IsHidden` (varsayılan **true** — güvenli taraf), `SetHidden`, `coverVisual` + `exposedVisuals[]`, `event OnHiddenChanged`.
  - **E tuşu bağlama göre anlam değiştiriyor:** gündüz kumar oynanamadığı için E **örtüyü** açar/kapatır; gece örtü kapalıysa önce açar, açıksa oynar. Ayrı bir tuş/obje gerekmedi.
  - `exposedVisuals`'a masanın tabelası konur, yoksa örtülü masanın tepesinde "BAHIS 100 TL" parlar.
- `PoliceManager` (singleton, `KanBagi.Illegal`) — **kayıt edilmez**, tamamen gün içi durum:
  - Gün başında zar: `SuspicionRatio < minimumSuspicionRatio` (0.2) ise hiç gelmez; değilse ihtimal `ratio × maxRaidChance` (0.6). `earliestRaidDay` 4'ten önce baskın yok.
  - Akış: dükkan açıkken rastgele bir saat → **`OnRaidWarning`** (20 sn süre) → memur gelir (`OnOfficerArrived`) → 12 sn denetim → `OnRaidResolved(busted, fine)`
  - Yakalanınca: para cezası (parası yetmezse elindeki kadarı), itibar −10, **şüphe sıfırlanır** (dosya kapandı). Temiz geçerse şüphe −15.
  - `FinePaidToday` — günlük özet bunu okur. Ceza `TrySpendMoney`'den geçtiği için zaten `Expenses`'ta; **`NetProfit`'te tekrar düşülmez**, satır sadece bilgi amaçlı (kumar/haraçtan farklı).
- `PoliceOfficer`: bilinçli olarak boş `Interact()` — memurla pazarlık yok, iş masayı zamanında örtmekti.
- `PoliceNotificationUI`: uyarı (sarı) / memur geldi (kırmızı) / sonuç mesajları.
- `PolicePrefabBuilder` (**Editor**): `Kan Bagi > Polis Prefabini Olustur` + **`Kan Bagi > Kumar Ortusunu Olustur ve Bagla`** (örtüyü üretip `coverVisual` alanına kendisi bağlar).
- `DayCycleManager`'a `OpenHour` / `CloseHour` getter'ları eklendi (baskın saatini seçmek için).

---

## 6.7 DENGE VE CİLA

### Adım 9a-1 — Gerçek değerler + ölçüm aracı ✅
- `BalancePresetApplier` (**Editor**, `KanBagi.EditorTools`): sahnedeki manager'lara tek tıkla değer yazar.
  - `Kan Bagi > Denge > Gercek Degerleri Uygula` / `Kan Bagi > Denge > Test Degerlerini Uygula`
  - Değerler dosyanın üstündeki **`Settings` tablosunda** (bileşen, alan, GERÇEK, TEST). Değiştirip menüye tekrar tıkla — Inspector gezmek yok.
  - SerializedObject ile yazar (Undo destekli), Console'a `obje > Bilesen.alan: eski -> yeni` raporu basar.
  - **Aynı singleton'dan birden fazla varsa** fazlasını siler (ilk objenin ilk bileşeni kalır).
  - `Managers`'ta `BalanceLogger` yoksa **kendisi ekler**.
  - Play modunda çalışmaz; diskte kayıt varsa "önce Kaydi Sil" uyarısı verir (kayıt başlangıç parasını ezer).
- `BalanceLogger` (`KanBagi.Core`, `Managers` üzerinde, **`[DefaultExecutionOrder(200)]`**): sadece event dinler, CSV günlüğü tutar.
  - `GUN` satırı (her `OnDayEnded`): para, itibar, şüphe, servis/kaçan, ciro, gider, kira, kumar, haraç, ceza, net.
  - `OLAY` satırı: oturum başlangıcı, itibar 10/20/30/50 ilk geçiş, upgrade/mülk alımı, mafya geldi/ödendi/reddedildi, polis uyarısı/sonucu.
  - Editor'de **`KAHVEHANEDENEME/DengeLog/denge_log.csv`** (proje kökü — Claude doğrudan okuyabilsin), build'de `persistentDataPath`. `DengeLog/` `.gitignore`'da.
  - Order 200 sebebi: SaveManager (100) kaydı yükledikten **sonra** abone olsun; yoksa kayıttan geri gelen upgrade/mülkler "yeni alındı" diye loglanırdı. Geçilmiş itibar barajları da açılışta işaretlenir.
- **Uygulanan GERÇEK değerler:** Starting Money 10000 → **250**, Starting Reputation 70 → **0**, Day Duration 1 → **6** dk, Delivery Delay 8 → **20**, Spawner ilk 2 → **3** sn / aralık (4,8) → **(6,14)**, Police: min şüphe 0 → **0.2**, max ihtimal 1 → **0.6**, denetim 3 → **12** sn, en erken gün 1 → **4**.
- **Yakalanan bug:** `Managers`'ta **iki `DayCycleManager`** vardı; çalışan kopya `nightTimeScale 1` olanıydı → **geceler hiç hızlanmıyordu**. Komut fazlasını sildi, artık gece 4× akıyor.
- Gerçek değerlerle zaman: 1 gün ≈ **4 gerçek dakika** (dükkan açık 3.5 dk, gece ~37 sn). Saat başına 15 sn (gündüz).

### Adım 9a-3 — Denge düzeltmeleri ✅ (ölçümsüz, kağıt tahminle)
**9a-3a — İflas koruması:**
- `PoliceManager.protectedMoney` (150): ceza artık `Clamp(kasa − 150, 0, fineAmount)` → polis kasada **en az 150 TL bırakır**. Kasa 150'nin altındaysa ceza 0, ama itibar −10 ve şüphe sıfırlama yine uygulanır.
- Veresiye/kurtarma mekaniği **bilinçli olarak yapılmadı** (basitlik). Polis dışındaki kilitler (kumarda batmak, stok 0 iken son parayla upgrade) oyuncunun kendi tercihi; ileride gerekirse gün başında "stok 0 + kasa < paket fiyatı" koşuluyla bedava paket yollayan küçük bir sınıf eklenebilir.

**9a-3b — Değerler** (gerekçe: itibar günde ~13, upgrade'lerden sonra ~18 artıyor; barajlar paranın yetiştiği günün hemen arkasına kondu ki her açılış hem para hem itibar istesin):

| Değer | Eski | Yeni | Nerede |
|---|---|---|---|
| Bakkal itibarı | 10 | **60** | `BakkalMulk.asset` |
| Terzi itibarı | 20 | **140** | `TerziMulk.asset` |
| Kıraathane itibarı | 30 | **240** | `KirathaneMulk.asset` |
| Gizli Kumarhane itibarı | 50 | **180** | `GizliKumarhaneUpgrade.asset` |
| Mafya barajı itibar / kasa | 30 / 2000 | **120 / 1000** | `BalancePresetApplier` |
| Haraç | 500 | **300** | `BalancePresetApplier` |
| Kızgın giden müşteri | −1 | **−2** | `Musteri` prefab (`reputationOnAngryLeave`) |
| Test modu başlangıç itibarı | 70 | **300** | `BalancePresetApplier` |

- `BalancePresetApplier` tablosuna `PoliceManager.fineAmount`, `protectedMoney` ve üç `MafiaManager` alanı eklendi.
- ⚠️ **Applier sadece sahnedeki bileşenlere yazar.** ScriptableObject (mülk/upgrade itibarı) ve prefab (müşteri) değerleri asset dosyasında durur; bunları değiştirmek için asset'i düzenle.
- Hedeflenen tahmini akış: Bakkal ~5–6. gün, Terzi ~9–10, mafya ilk ziyaret ~10, Kumarhane ~11, Kıraathane ~14. **Ölçülmedi** — `BalanceLogger` CSV'si ile doğrulanabilir.
- Kumarda oyuncunun kendi oynadığı elin beklentisi (−10 TL) bilinçli olarak değiştirilmedi; 9a-2 (kumarhane işletmesi) bu dengeyi değiştirecek.
- **Test:** tabelalar ve PC'de "Itibar 60 / 140 / 240 / 180 gerekli" görünüyor, kızgın müşteri −2 itibar düşürüyor (kullanıcı onayladı).

### Adım 9b — Co-op hazırlığı: etkileşim katmanı ✅
Davranış birebir aynı; amaç her etkileşimin "hangi oyuncu yaptı" bilgisini taşıması.

**9b-1 — PlayerContext + yeni IInteractable:**
- `PlayerContext` (`KanBagi.Player`, **düz C# sınıfı**, sahneye eklenmez): `Hands` (PlayerHands), `Lock` (PlayerLock).
- `PlayerInteractor.Context` — `Awake`'te kendi objesindeki `PlayerHands` / `PlayerLock`'tan kurulur, her `Interact(Context)` çağrısına geçirilir.
- `IInteractable` → `string GetPrompt(PlayerContext player)` + `void Interact(PlayerContext player)`.
- 12 implementer güncellendi. İstasyonlar (`GlassShelf`, `TeaStove`, `Sink`, `Table`, `StorageRack`, `HoldableItem`) eli `player.Hands`'ten alır; oyuncuyla işi olmayanlar (`GamblingTable`, `MafiaCollector`, `PoliceOfficer`, `PropertyBuilding`) parametreyi yok sayar.
- `InteractionPromptUI` → `interactable.GetPrompt(interactor.Context)`.

**9b-2 — Singleton'lar kalktı:**
- `PlayerHands` ve `PlayerLock`'tan `Instance` kaldırıldı.
- `ComputerStation`: ekranı açanın kilidini (`screenUser`) hatırlar, sadece onu kilitler/açar. **Ekran açıkken başkası devralamaz.**
- `HeldItemUI`: opsiyonel `hands` alanı; boşsa `FindFirstObjectByType<PlayerHands>()` (co-op'ta her HUD kendi oyuncusuna bağlanacak).
- `DailyReportUI`: gün sonunda **sahnedeki tüm `PlayerLock`'ları** kilitler, "Yeni Gun"de hepsini açar.
- `Billboard`'daki `Camera.main` **bilinçli olarak kaldı**: tabelayı kameraya çevirmek her istemcinin kendi ekranında yapılan iş.

**⚠️ Yeni kural:** Yeni bir etkileşimli nesne yazarken `IInteractable`'ı `PlayerContext` ile uygula; oyuncunun eline/kilidine **asla** global erişimle değil `player.Hands` / `player.Lock` ile ulaş.

**Test:** Faz 1-4 akışlarının tamamı (bardak, ocak, servis, lavabo, koli, PC + ESC, satın alma, sokakta mülk, kumar örtü/oyun, haraç, gün sonu kilidi, "Elinde" yazısı) kullanıcı tarafından denendi, çalışıyor.

### Adım 9a-2 — Kumarhane işletmesi ✅ (NPC kumarbazlar)
Gece arka odaya NPC kumarbazlar gelir, masaya oturup el oynar; **kasa her elden pay alır**, şüphe artar. Oyuncunun kendi oynadığı mod aynen duruyor.

- `Gambler` (`KanBagi.Illegal`, coroutine durum makinesi): kapıdan girer → sandalyeye yürür → oturur (agent kapanır, masaya döner) → **4–8 el**, el arası **4 sn** → kalkıp çıkar, kendini yok eder. Para/şüpheye dokunmaz, sadece `table.PlayHouseRound()` çağırır. Oyuncuya özel hiçbir şey yok (co-op uyumlu).
- `GamblerSpawner` (**KumarMasasi'nın üzerinde**, `[RequireComponent(GamblingTable)]`): masa upgrade'le açılana kadar pasif olduğu için kumarhane açılmadan kimse gelmez. Gece + örtü açık + boş sandalye + gün sonu paneli kapalıysa **3–6 sn**'de bir kumarbaz yollar. **Örtü kapanınca, gün bitince ve sabah dükkan açılınca herkes kalkar.**
- `GamblingTable` eklemeleri:
  - `PlayHouseRound()` — kumarhane parasının **tek geçiş noktası** (ileride RPC sınırı): `houseRakePerRound` (**25 TL**) kasaya, `suspicionPerHouseRound` (**+1**) şüpheye, `NightNet`'e eklenir, `OnHouseRound` tetiklenir. Örtü kapalıysa veya sabahsa el sayılmaz.
  - Sandalyeler **elle verilmez**: masanın collider'ının dört kenarında aday noktalar üretilir, NavMesh'te yürünebilir olanlardan en fazla `maxSeats` (**2**) tanesi seçilir. Mevcut sahnede sandalyeler masanın **güney kenarında** (duvar/Depo tarafı dolu). Seçili masada mor küre gizmo'su.
- `GamblingSignUI`: kumarbaz eli gelince "KASA +25 TL" yazar, gecelik toplam güncellenir.
- `DailyStatsManager`: kumarhane payı günlük özetteki **"Kumar"** satırına eklenir (`AddMoney` ile girdiği için `Expenses` düzeltmesi gerekmez).
- **Gece hızı 4× → 2×** (`DayCycleManager.nightTimeScale`, test modunda 1): gece kumarhanesi için geceler uzadı. `BalancePresetApplier` tablosuna gece hızı, spawn aralığı (gerçek 3–6 / test 2–3), masa payı ve şüphe eklendi.

**Editor komutları:**
- `Kan Bagi > Kumarhaneyi Kur` (`GamblerPrefabBuilder`): `Kumarbaz` prefab'ını kodla üretir (koyu yeşil kapsül + NavMeshAgent + Gambler, müşteriyle aynı yürüme ayarları), sahnedeki her kumar masasına `GamblerSpawner` ekleyip prefab'ı ve giriş noktasını (müşteri spawner'ının `GirisNoktasi`) bağlar.
- `Kan Bagi > NavMesh Kontrol Et` (`NavMeshChecker`): her eşyanın altında NavMesh deliği var mı raporlar.
- `Kan Bagi > NavMesh'i Yeniden Bake Et` (**asıl çözüm**): her eşyaya "Not Walkable" `NavMeshModifier` ekler, kilitli (pasif) eşyaları **sadece bake anında** açar, engelli objeleri de bake'e alır (`ignoreNavMeshObstacle = false`). Delikler NavMesh dosyasına kalıcı yazılır. **Eşya taşınınca/eklenince tekrar çalıştır + sahneyi kaydet.**
- `Kan Bagi > NavMesh Deliklerini Duzelt`: eski yöntem (oyma yapan `NavMeshObstacle`). Artık gerekmiyor; eklediği engeller sahnede zararsız duruyor.

**Yakalanan bug:** Kumarbazlar kumar masasının **üstünden yürüyordu**. Sebep: masa bake sırasında pasifti → altı yürünebilir kalmıştı; sonradan eklenen oyma engeli ise NavMeshSurface "engelli objeleri bake'e alma" ayarı yüzünden bake'e hiç girmiyordu, delik tamamen çalışma anındaki oymaya kalmıştı. Yeniden bake komutu ile çözüldü; `NavMesh Kontrol Et` 11 eşyanın hepsinde delik raporluyor. (Bake'te "TextMeshPro Mesh has invalid vertex data" uyarıları zararsız — tabela yazıları atlanıyor.)

**Test:** kumarbazlar gece geliyor, masanın etrafından dolaşıp oturuyor, el oynuyor; kasa ve şüphe artıyor, günlük özette "Kumar" satırı doğru (test gününde 4 el → +100 TL, şüphe +4). Kullanıcı onayladı.

---

## 6.8 KİMLİK VE İÇERİK

### Adım 10a-1 — Hikaye giriş ekranı ✅
Yeni oyunda (diskte kayıt yokken) intikam hikayesi siyah tam ekranda sayfa sayfa anlatılır. Kayıtlı oyunda hiç görünmez.

- `StoryData` (`KanBagi.Data`, ScriptableObject, `Create > Kan Bagi > Story Data`): `StoryPage` listesi (başlık + metin) ve buton yazıları (`continueLabel`, `skipLabel`, `finishLabel`).
- `GirisHikayesi.asset`: 5 sayfa — **İki Ortak, Taziye Çayı, Dört Duvar, Miras, Hesap**. Anlatıcı "biz" (co-op'ta iki kardeşe de oturuyor). Butonlar **Devam / Atla / Ocağı Yak**. **Türkçe metin kodda değil bu asset'te** (kod ASCII kuralı korunuyor); değiştirmek için Inspector'dan düzenle.
- `DayCycleManager.SetPaused(bool)` + `IsPaused`: duraklatılınca saat ilerlemez (co-op'ta RPC sınırı).
- `StoryIntroUI` (`KanBagi.UI`, panel kökünde, kök hep aktif): `Start`'ta `SaveManager.HasSave` yoksa açılır → saati durdurur, sahnedeki **tüm** `PlayerLock`'ları kilitler. **Devam** sonraki sayfa, **Atla** direkt başlatır (son sayfada gizlenir), son sayfadaki **Ocağı Yak** paneli kapatır, saati ve oyuncuları serbest bırakır. Sayfa numarası "1 / 5".
- `Kan Bagi > Hikaye Giris Panelini Olustur` (`StoryPanelBuilder`): `HikayeGirisPaneli` prefab'ını kodla üretir, `GirisHikayesi`'ni bağlar, **sahnedeki ana Canvas'ın son çocuğu olarak kendisi yerleştirir** (eskisi varsa siler). Panel 1300×720, başlık 60 / metin 38 punto (Canvas 1920×1080 referansla ölçekleniyor).

**Test:** `Kaydi Sil` → Play: ekran açılıyor, saat 00:00'da duruyor, oyuncu kilitli; 5 sayfa geçiliyor, "Ocağı Yak" ile oyun başlıyor; "Atla" direkt başlatıyor; kayıt varken ekran gelmiyor. Kullanıcı onayladı.

### Adım 10a-2a — Mahalle fısıltıları ✅
Ekranın üst ortasında, intikam tonunda kısa bir satır belirir (italik, amber, 6 sn). Sadece event dinler, hiçbir oyun durumunu değiştirmez.

| Olay (event) | Fısıltı |
|---|---|
| Gün 1 dükkan açılınca (`OnShopOpened`, day == 1) | Babanın ocağı soğuk. Bardakları al, çayı demle. Mahalle seni izliyor. |
| Tahsildar gelince (`OnDemandArrived`) — **her gelişte** | Kapıda takım elbiseli biri var. Mahallede para dönüyorsa, payını istiyorlar. |
| Haraç ödenince / reddedilince | Ödedin. Şimdilik. Bu adamlar doymaz. / Haracı ödemedin. Bu mahallede bunun bir bedeli olur. |
| Mülk alınınca (`{0}` mülk adı) | {0} artık bizim. Amcanın sokağından bir taş daha söküldü. |
| Gizli Kumarhane upgrade'i alınınca | Arka oda hazır. Kepenk inince masa açılır. |
| İtibar 60 / 140 / 240'ı **aşağıdan yukarı** geçince | Esnaf artık selam veriyor. / Kahvede adın konuşuluyor. / Amcanın kulağına adın gitmiş. |

- `WhisperData` (`KanBagi.Data`, `Create > Kan Bagi > Whisper Data`) + `MahalleFisiltilari.asset`: tüm metinler (Türkçe karakterli), kumarhane `UpgradeData` referansı, itibar barajı listesi (`threshold` + `message`).
- `WhisperUI` (`KanBagi.UI`, **`[DefaultExecutionOrder(200)]`**): DayCycle, Mafia, Property, Upgrade ve Reputation event'lerini dinler; eksik manager sadece kendi fısıltısını susturur. Mesajlar **kuyrukla** sırayla gösterilir. Order 200 sayesinde kayıt yüklendikten **sonra** abone olur → kayıttan geri gelen mülk/upgrade ve önceden geçilmiş itibar barajları tekrar fısıldanmaz (BalanceLogger ile aynı yöntem).
- `Kan Bagi > Fisilti Satirini Olustur` (`WhisperLineBuilder`): `FisiltiText`'i ana Canvas'a kurar (üst orta, Pos Y −70, 1000×60), veriyi bağlar, eskisini siler; **giriş ve gün sonu panellerinin altına** sıralar ki onlar açıkken üste binmesin.
- **Bilinçli sapma:** mafya mesajı planda "ilk kez" idi, her gelişte gösteriliyor (sadece ilk seferi ayırmak kayda bayrak yazmayı gerektiriyordu; cümle her ziyarete oturuyor). Gerekirse ileride kayda bayrak eklenir.

**Test:** ilk sabah mesajı, mülk ve kumarhane alımı, tahsildar geliş/ödeme/ret mesajları görünüyor; kayıttan devam edince eski olaylar tekrar fısıldanmıyor. Kullanıcı onayladı. (Test modunda itibar 300'le başladığı için baraj mesajları gelmez — gerçek değerlerle görülür.)

### Adım 10a-2b — Mevcut metinlerin tonu ✅
Polis, teslimat, tahsildar, kumar, mülk ve gün sonu metinleri fısıltılarla aynı tona çekildi. **Kapsam (kullanıcı kararı): sadece tablodaki metinler** — geri kalan arayüz ASCII kaldı ("Itibar", "Gun", "YENI GUN"...).

| Bileşen.alan | Yeni metin |
|---|---|
| `PoliceNotificationUI.warningMessage` | Köşede ekip arabası var! Masayı ört, çabuk! |
| `PoliceNotificationUI.arrivedMessage` | Memur içeride, gözü her yerde. |
| `PoliceNotificationUI.bustedFormat` | Bastılar! {0} TL ceza, itibarın yerlerde. |
| `PoliceNotificationUI.cleanMessage` | Memur eli boş döndü. Bu sefer. |
| `PoliceOfficer.idlePrompt` | Memur etrafı kolaçan ediyor |
| `MafiaCollector.idlePrompt` / `payFormat` | Takım elbiseli adam bekliyor / Haracı öde ({0} TL) |
| `GamblingTable.playFormat` / `hideMessage` / `revealMessage` | Masaya otur ({0} TL) / Masayı ört / Örtüyü kaldır |
| `GamblingSignUI.houseFormat` | KASAYA +{0} TL |
| `DeliveryNotificationUI.queuedFormat` / `deliveredFormat` | Sipariş verildi: {0} x{1}, kurye yolda / Kurye bıraktı: {0} x{1} kapıda |
| `PropertyBuilding.ownedFormat` | Artık bizim - Günlük {0} TL |
| `DailyReportUI.titleFormat` | GÜN {0} — HESAP DEFTERİ |
| `noMoneyFormat` / `notEnoughMoneyFormat` (tahsildar, kumar, bina) | {0} TL lazım |

- `TextPreset` (`KanBagi.Data`, `Create > Kan Bagi > Text Preset`) + **`ArayuzMetinleri.asset`**: `FieldText` listesi (bileşen adı + alan adı + metin). **Arayüz metinlerinin tek kaynağı** — Türkçe karakter burada, kodda değil.
- `Kan Bagi > Metinleri Uygula` (`TextPresetApplier`): önce `_Project/Prefabs` altındaki **prefab'lara** (`LoadPrefabContents` → kaydet), sonra açık **sahneye** (pasifler dahil, Undo destekli) yazar. Console'a `obje > Bilesen.alan: eski -> yeni` raporu; tekrar çalıştırılınca "0 alan degisti". İlk çalıştırmada 22 alan değişti (Tahsildar, PolisMemuru, KumarTabelasi, GunlukOzetPaneli prefab'ları + sahnede polis/teslimat bildirimi, kumar masası, 3 bina).
- **Uygulanmayan satır:** emlak tabelasındaki "SENIN - Gunluk" metni prefab'da artık kullanılmayan **eski bir alanda** (`PropertySignUI` bu alanı artık okumuyor; tabela mülk alınınca kayboluyor). Ekranda görünmediği için atlandı.
- ⚠️ **Builder prefab'ı yeniden üretirse** (ör. `Polis Prefabini Olustur`, `Kumar Tabelasini Olustur`, `Gunluk Ozet Panelini Olustur`) metinler koddaki ASCII varsayılanlara döner → **`Metinleri Uygula`'yı tekrar çalıştır.**
- **ASCII kalanlar (→ 11 cila):** Inspector'daki diğer metinler ve **kodun içinde üretilen istemler** ("Temiz Bardak Al", "Elinde: ...", "Koliyi Bosalt", "{X} Doldur", "Elinde bos bardak olmali"). Tam Türkçe için bunlar da `ArayuzMetinleri`'ne / alanlara taşınmalı.

**Test:** gün sonu başlığı, teslimat bildirimleri, kumar masası istemleri ve tabela, tahsildar, polis baskını mesajları ve sahip olunan bina istemi yeni metinlerle görünüyor. Kullanıcı onayladı.

### 10c planı — Dükkan genişletme ve çeşitlilik (onaylandı, 2026-10-15)
| Adım | İçerik |
|---|---|
| **10c-1** ✅ | Müşteri profilleri (renk, sabır, zevk, bahşiş, itibarla açılma) |
| **10c-2** ✅ | Masa oyunları: İskambil ve Domino upgrade'leri okey gibi masada belirir; oyunlu masadaki müşteri içtikten sonra **bir tur daha** sipariş verebilir (+ profillere "oyun merakı") |
| **10c-3** ✅ | Yeni içecek: Sahlep (Sahlep Makinesi upgrade'i + Sahlep Tozu) — veri + genel `Icecek Istasyonlarini Kur` builder'ı |
| ~~**10c-4**~~ ➜ | ~~Bahçe: sabit zemin + 2 sabit masa~~ — **İPTAL, yeni "Dükkan Düzenleme" bölümüne taşındı** (bahçe orada bir genişletme bölgesi; masaları oyuncu yerleştirir). Bkz. bölüm 6.9 |

Sıranın mantığı: profiller her şeyin temeli → masa başı gelir (ikinci tur) → menü çeşitliliği → en son fazla masa (talep oluştuktan sonra anlamlı).

### Adım 10c-1 — Müşteri profilleri ✅
Bütün müşteriler artık aynı değil: her birinin tipi var, tipine göre renk, sabır, zevk ve bahşiş değişiyor; bazı tipler itibarla açılıyor.

| Profil (asset) | Renk | Sabır | Zevk (ağırlık) | Bahşiş | Kızarsa | Görünür | Gelme ağırlığı |
|---|---|---|---|---|---|---|---|
| `EmekliDayi` | gri | ×1.5 | Çay 4, **Sahlep 2** (10c-3), Oralet 1 | 0 | −1 | baştan | 3 |
| `Ogrenci` | mavi | ×1.0 | Oralet 3, Çay 2, **Sahlep 1** (10c-3) | 0 | −1 | baştan | 3 |
| `Esnaf` | kahverengi | ×0.7 | Kahve 3, Çay 1 | +5 TL | −2 | itibar 60 | 2 |
| `Kabadayi` | siyah | ×0.6 | Çay 2, **Sahlep 1** (10c-3) | +10 TL | **−4** | itibar 140 | 1 |

- `CustomerProfile` (`KanBagi.Data`, `Create > Kan Bagi > Customer Profile`): `bodyColor`, `patienceMultiplier`, `drinkDurationMultiplier` (hepsinde 1), `favoriteDrinks` (`DrinkPreference`: içecek + ağırlık), `tip`, `reputationOnAngryLeave`, `requiredReputation`, `spawnWeight`. Asset'ler `ScriptableObjects/MusteriProfilleri/`.
- `CustomerSpawner.profiles`: itibarı yeten profiller arasından **ağırlıklı** seçim, `customer.Initialize(exit, profile)`. Liste boşsa / uygun profil yoksa eski davranış (prefab varsayılanları).
- `Customer`:
  - Renk `MaterialPropertyBlock` ile (`_BaseColor` + `_Color`) — malzeme kopyalanmaz.
  - Sipariş: profilin favorilerinden **menüde olanlar** arasında ağırlıklı; hiçbiri menüde yoksa `MenuManager.GetRandomBeverage()`.
  - Sabır = temel × profil × `CustomerModifiers` (okey). İçme süresi × profil çarpanı.
  - `PaidAmount = fiyat + bahşiş` → **bahşiş günlük cironun içinde**.
  - Kızgın gidiş cezası profilden gelir (prefab'daki `reputationOnAngryLeave` sadece profilsiz müşteride kullanılır).
- `Kan Bagi > Musteri Profillerini Bagla` (`CustomerProfileLinker`): projedeki **tüm** `CustomerProfile` asset'lerini Spawner'a itibar sırasıyla bağlar. **Yeni profil eklenince tekrar çalıştır + kaydet.**
- ⚠️ **Denge etkisi:** 9a-3'te kızgın müşteri herkes için −2 yapılmıştı; artık profilden geliyor → erken oyunda (Dayı/Öğrenci) −1. Erken oyun biraz daha affedici.

**Test:** gerçek değerlerde sadece gri/mavi geliyor, oralet yokken öğrenci çay istiyor, dayı daha sabırlı; test değerlerinde kahverengi ve siyah da geliyor, esnaf kahve istiyor, kabadayı 25 TL ödüyor ve bekletilince −4 itibar; ciro bahşişi içeriyor. Kullanıcı onayladı.

### Adım 10c-2 — Masa oyunları ve ikinci tur ✅
İskambil ve Domino upgrade'leri okey gibi **bütün masalarda** belirir (dükkan geneli etki, masa bazlı değil). Oyunlu dükkanda müşteri içtikten sonra belli bir şansla **kalkmaz, bir tur daha** sipariş verir → masa başı gelir artar.

| Upgrade (asset) | Fiyat | İtibar | Etki |
|---|---|---|---|
| Okey Takımı (mevcut) | 150 | — | Sabır ×1.5 (değişmedi) |
| `IskambilUpgrade` — İskambil Destesi | 200 TL | 40 | İkinci tur şansı **+%30** |
| `DominoUpgrade` — Domino Takımı | 350 TL | 100 | İkinci tur şansı **+%20** (ikisi birlikte %50) |

**Oyun merakı** (`CustomerProfile.gameCuriosity`, ikinci tur şansı bununla çarpılır): Emekli Dayı ×1.5, Öğrenci ×1, Esnaf ×0.5, Kabadayı ×1 → iki oyun alınmışken Dayı %75, Öğrenci/Kabadayı %50, Esnaf %25.

**İkinci tur kuralları:**
- İçme biter, bardak masada **kirli** kalır; zar tutarsa müşteri profil zevkine göre **yeni** sipariş verir (balon tekrar açılır).
- Oyuncu önce kirli bardağı kaldırmalı (mevcut `Table` kuralı: "Once masadaki bardagi kaldir").
- **En fazla bir ekstra tur.** İkinci turda sabrı biterse / kapanış gelirse **itibar cezası yok** (zaten memnun).
- İkinci servis yine fiyat + bahşiş + 1 itibar; `PaidAmount` **birikir** → günlük özette müşteri 1, ciro iki satışın toplamı.

**Parçalar:**
- `CustomerModifiers.SecondRoundChance` + `AddSecondRoundChance(float)` — paylar **toplanır**, en fazla 1.
- `TableGameBoost` (`KanBagi.Customers`, **Managers** üzerinde, `CustomerPatienceBoost` deseni): upgrade alınınca / kayıttan gelince payı bir kez ekler. İskambil (0.3) ve Domino (0.2) için iki tane.
- `Customer`: `WantsSecondRound()` (dükkan payı × oyun merakı) → `SecondRoundRoutine()`. Inspector'da sabit sipariş atanmışsa (`hasFixedOrder`) ikinci turda da o istenir.
- `Kan Bagi > Masa Oyunlarini Kur` (`TableGamesBuilder`, tekrar çalıştırılabilir):
  - Her masaya (kilitli Masa3 dahil) `IskambilDestesi` (kırmızı, yerel (−0.3, 0.52, 0.3)) ve `DominoTakimi` (fildişi, yerel (0.3, 0.53, −0.3)) koyar — **collider yok**, başta pasif. Malzemeler `Materials/IskambilDestesi.mat`, `DominoTakimi.mat`.
  - `Managers`'a her oyun için `UpgradeUnlocker` + `TableGameBoost` ekler/günceller (aynı upgrade'e bağlı olanı bulur, çoğaltmaz).
  - İki upgrade'i `GelistirmeKatalogu`'na ekler (PC + kayıt eşlemesi).

**Test:** PC'de iki yeni upgrade görünüyor, alınınca masalarda deste ve domino beliriyor; müşterilerin bir kısmı ikinci balonla sipariş veriyor, kirli bardak kaldırılmadan yeni bardak konmuyor, ikinci turda bekletilen müşteri itibar düşürmeden gidiyor, ciro iki satışı içeriyor, kayıttan etki geri geliyor. Kullanıcı onayladı.

### Adım 10c-3 — Sahlep ✅
En pahalı içecek: **Sahlep 30 TL** (çay 15, oralet 18, kahve 25), hazırlama 4 sn (Sanayi Ocağı ile 2.4). **Sahlep Tozu** 10'lu paket 50 TL (birim 5) → bardak başı kâr 25 TL. **Sahlep Makinesi** upgrade'i **300 TL, itibar 80**. Makine arka tezgah hattında, Lavabo'nun sağında **(3.5, 0.5, 3)**, krem rengi, başta pasif.

**Zevkler:** Dayı Çay 4 / Sahlep 2 / Oralet 1, Öğrenci +Sahlep 1, Kabadayı Çay 2 / Sahlep 1, Esnaf değişmedi. Makine alınmadan sahlep menüde olmadığı için kimse istemez ("sadece menüdeki favoriler" kuralı).

**Yeni bir içeceğin oyuna tam girmesi 10 bağlantı ister** (kahve/oralet elle yapılmıştı, biri unutulursa sessizce bozulur): içecek `ItemData`, malzeme `ItemData`, `UpgradeData`, geliştirme kataloğu, malzeme kataloğu (upgrade'e kilitli), **ürün veritabanı (kayıt!)**, `MenuManager` (upgrade'e kilitli), sahnede `TeaStove` + `UpgradeUnlocker`, Sanayi Ocağı listesi (`StationSpeedBoost`), `StockUI`. İlerleme çubuğu istasyonları kendisi bulur.

- Veri: `Sahlep.asset`, `SahlepTozu.asset`, `SahlepMakinesiUpgrade.asset` (Türkçe metinler asset'te).
- **`Kan Bagi > Icecek Istasyonlarini Kur` (`BeverageStationBuilder`, genel):** dosyanın başındaki **`Stations` tablosundaki** her içecek için 3–10 arası bağlantıların hepsini kurar; raporu `eklendi / zaten vardi` diye basar. Var olan istasyonu `beverageData`'dan tanır, **tekrar çalıştırılabilir, çoğaltmaz.** Kahve/oralet tabloda yok (elle kurulu, dokunulmadı). **Yeni içecek = veri asset'leri + tabloya bir satır + menü + NavMesh bake.**
- `TableGamesBuilder`'daki `FindByUpgrade`, `AddToCatalog`, `GetOrCreateMaterial` `internal` yapıldı; yeni builder kopyalamadan kullanıyor (davranış değişmedi).
- Kurulumdan sonra `NavMesh'i Yeniden Bake Et` → `NavMesh Kontrol Et`: **12 eşyanın hepsinde delik var.**
- **Oyun kodu değişmedi** — `TeaStove`, menü, stok ve kayıt sistemleri sahlebi olduğu gibi kaldırdı.

**Test:** gerçek değerlerde "Sahlep Tozu: 0" görünüyor, malzeme ve makine kilitli; makine alınınca beliriyor, toz sipariş edilip depoya konunca sahlep yapılıyor, profiller sahlep istiyor, 30 TL (+ bahşiş) giriyor; Sanayi Ocağı hızlandırıyor; kayıttan makine, stok ve menü geri geliyor. Kullanıcı onayladı.

---

## 6.9 DÜKKAN DÜZENLEME — YENİ BÖLÜM (kararlar 2026-10-17, mahalleden ÖNCE)

> **Neden:** Kullanıcının upgrade vizyonu "kilitli sahne" değil, **"kendi dükkanını kur"** (Supermarket Simulator / cafe sim türü). Bugünkü model (eşyalar sahneye elle sabit konmuş, upgrade sadece `UpgradeUnlocker` ile obje AÇIYOR) bu vizyonla uyuşmuyor. **Mahalleden önce** yapılır çünkü: *dükkanın ulaşabileceği en büyük boyut belli olsun ki mahalle onun etrafına kurulsun* (kullanıcının gerekçesi).

**Kullanıcı kararları:**
1. **Yerleştirme serbest:** mümkün olduğunca oyuncuya kalsın — **0.1–0.2 m adım**, **istediği açıda döndürme** (90° ile sınırlı değil).
2. **Bütün eşyaların yeri değiştirilebilir** (masa, ocak, makineler, lavabo, raf, kumar masası…).
   - **Kumar için ayrı oda:** geliştirme sayfasında **"Kumar Odası"** olarak görünür → alınca oda açılır → **kumar masası ayrıca satın alınır** ve **sayısı artırılabilir** (birden fazla kumar masası).
3. **Teslimat iki türlü:**
   - **Sarf malzemesi** (çay yaprağı, kahve çekirdeği…) → bugünkü gibi **kurye + koli**.
   - **Eşya** (masa, kahve makinesi…) → alınınca **elde hayalet (yarı saydam önizleme) olarak belirir**, yerleştirilince normal haline döner. Kurye yok.
4. **Genişletme hazır bölgelerle:** duvar kaldırılınca yeni bölge açılır. **Sıralı açılır** — "direkt üst katı alayım" olmaz. Örnek sıra: **yan oda → bahçe → üst kat** (kumar odasının bu zincirdeki yeri netleştirilecek).
5. **Grup müşteriler:** **2, 3 veya 4 kişi** gelebilir (ör. 4 kişi gelip okey oynar).
   **Tepsi:** **8 bardak** alır; **dolu bardak servisi + masadaki boş/kirli bardakları toplama** ikisi de tepsiyle yapılır.
6. **Sıra:** **tepsiden başla**, bu bölüm **mahalleden (10d) önce**.

**Bonus vizyon — kıraathaneden cafeye:** dükkan zamanla kıraathane olmaktan çıkıp **cafeye** dönüşecek. Bunun için **mobilya kataloğu** ve **kişiselleştirme** gerekecek: **duvar kâğıdı, zemin, mobilyalar** — oyuncuya "burası benim dükkanım" hissi vermek için.

**Taslak adımlar (her biri ayrıca planlanıp onaylanacak):**

| # | Adım | İçerik |
|---|---|---|
| **D1** ✅ | **Tepsi** | 8 bardak; raftan bardak alma, ocakta doldurma, masaya servis, kirli toplama, lavaboda toplu yıkama. Bağımsız iş, ilk adım. |
| D2 ✅ | Eşya altyapısı | `FurnitureData` (prefab, fiyat, kapladığı alan, kategori), yerleştirilmiş eşyaları tutan `FurnitureManager` (dünya durumu → co-op'ta server), **kayıt** (tür + konum + açı), **oyun içi NavMesh güncellemesi** (editörde bake yerine). Önce masalar. **Oyuncuya görünür bir şey getirmez — sadece altyapı.** |
| D3 ✅ | Yerleştirme modu | Hayalet önizleme, 0.1 m adım, 5° döndürme (Shift 45°), çakışma/zemin/oturma noktası kontrolü, var olan eşyayı tek tek alıp taşıma; "Yeni Masa" elde hayalet. |
| D4 | Eşya satın alma | PC'de eşya kataloğu → alınca elde hayalet. Sarf malzemesi kurye ile kalır. |
| D5 | Bütün eşyalar taşınabilir | Ocak, makineler, lavabo, raf, PC… Sabit sahne + `UpgradeUnlocker` düzeni kalkar; menü kilidi "upgrade alındı mı" yerine "makine dükkanda mı" olur. |
| D6 | Duvarlar + sıralı genişletme bölgeleri | İç/dış ayrımı (10d'den buraya kaydı), kapı. Yan oda → bahçe → üst kat; **Kumar Odası** + çoğaltılabilir kumar masası. **Dükkanın en büyük boyutu burada kesinleşir.** |
| D7 | Çok kişilik masalar + grup müşteriler | 2'lik/4'lük masalar, 2–4 kişilik gruplar; birlikte gelir/gider, her kişi kendi siparişi. |
| D8 | Masa oyunları masa bazında | Okey/iskambil/domino masaya konan eşya; 4 kişilik okey grubu okeyli masayı ister (10c-2'deki dükkan geneli etki buna dönüşür). |
| D9 | Kişiselleştirme / cafe | Mobilya kataloğu, duvar kâğıdı, zemin. (Dükkan boyutunu etkilemediği için mahalleden sonraya da kayabilir — kullanıcıya sorulacak.) |

### Adım D1 — Tepsi ✅
Tek bardak taşıma aynen duruyor; tepsi **ek** bir yol. Tepsi **8 bardak** (4×2) alır: dolu, boş veya kirli.

**Akış (elde tepsiyle E):**

| Nerede | Ne olur |
|---|---|
| Rafın üstündeki tepsi | Bakıp E → tepsi ele (`HoldableItem`, "Tepsiyi Al") |
| **Bardak rafı** (gövdesi) | **Her E bir temiz bardak** tepsiye: "Tepsiye Bardak Koy (3/8)" |
| **Ocak / makine** | Tepsi ocağa iner, **bir** boş bardak dolmaya başlar. Ocak çalışırken **her E bir bardak daha sıraya** ("Bir Çay Daha (2 sırada)"). Malzeme **her bardakta ayrı** düşer; stok biterse sıra durur. Bitince tepsiye bakıp alırsın (dolum sırasında tepsi alınamaz). Tek bardak dolduysa elde tepsiyle "Bardağı Tepsiye Al" |
| **Masa** | **Tek basış:** önce masadaki kirli/boş bardak tepsiye, sonra bekleyen müşteriye **istediği** dolu bardak ("Kirliyi Topla + Servis Et"). Müşteri içerken bardak dolu sayılır, toplanmaz. Tepside yoksa "Tepside Kahve yok" |
| **Lavabo** | Tepsideki **tüm kirli + dolu (dökülür)** bardaklar toplu yıkanır, **bardak başı 1.5 sn** (tekli 2.5 sn — toplu indirim). Temiz-boş bardaklar tepside kalır. Bitince tepsiye bakıp alırsın |
| **Tepsi yeri** (rafın üstü, tepsi yokken) | "Tepsiyi Bırak" |

**Parçalar:**
- `Tray` (`KanBagi.Items`, `[RequireComponent(HoldableItem)]`): `capacity` 8, `grid` 4×2, `spacing` (0.1, 0.12), `surfaceHeight`, `glassLift`. Yuvalar `Awake`'te çocuk transform olarak oluşur; bardak `HoldableItem.AttachTo(yuva)` ile yapışır → **`IsHeld = true`, collider kapalı** (ışın tepsiye çarpar, masa/ocak yuvaları bardağı "kaldırılmış" görür). `TryAdd`, `TryTakeOut`, `TryRemoveAndDestroy`, `CountByState`, `FindByState`, `Find`, `FindAll`, `GetSummary` ("3 Çay, 1 Kahve, 2 boş, 1 kirli"), `OnContentsChanged` (tepsideki bardağın durumu değişince de tetiklenir).
- `TraySpot` (`KanBagi.Shop`): ince collider; tepsi üstündeyken ışın tepsiye, kalkınca buraya çarpar. Doluluk takibi gerekmiyor. **Şimdilik bardak rafının üstünde sabit** (2 adet, `TepsiYeri1/2`); D3'te tezgaha da konabilecek.
- İstasyonlara **tepsi dalı** eklendi, tek bardak kodu korunarak: `GlassShelf` (`SpawnGlass` ortak), `TeaStove` (`pendingFills` sırası, `TrayOnStove` — tepsi geri alınınca kendini temizler), `Table` (`ServeWithTray`, `PlaceAndServe` ortak), `Sink` (`trayWashDurationPerGlass` 1.5, `TrayOnSink`).
- `HeldItemUI`: "Elinde: Tepsi (4/8) - 3 Çay, 1 boş".
- `Kan Bagi > Tepsiyi Kur` (`TrayBuilder`): `Tepsi` prefab'ını kodla üretir (**kök ölçeksiz** + kökte BoxCollider 0.45×0.03×0.32, `Govde` çocuğu tahta renkli, `HoldableItem` "Tepsi"/"Tepsiyi Al" + `Tray`; `glassLift` bardak prefab'ının mesh yüksekliğinden). Rafın `Renderer.bounds`'undan **2 tepsi yeri + 2 tepsi** (co-op: oyuncu başına bir) koyar. Tekrar çalıştırılabilir.
- `NavMeshChecker`: **`HoldableItem` taşıyan objeler mobilya sayılmaz** (tepsi/koli bake'te yürünemez işaretlenmesin).
- ⚠️ Kök ölçeksiz kuralı: bardak tepsinin çocuğu olduğu için tepsinin kökü ölçeklenirse bardaklar ezilir. Gövde ayrı çocuk.
- Bilinen küçük görsel kusur: ocak/lavabo yuva noktası bardak ortasına göre ayarlı; pivotu altta olan tepsi yüzeyden birkaç cm yukarıda durur (11'de).
- Co-op: tepsi oyuncunun elinde (`player.Hands`), istasyonlar dünya nesnesi.

**Test:** raftan tepsiyle bardak alma, ocakta sıralı doldurma, karışık sipariş (çay + kahve), masada kirli toplama + servis (ikinci tur dahil), lavaboda toplu yıkama, tepsiyi geri bırakma ve tepsisiz tek bardak servisi çalışıyor. Kullanıcı onayladı.

### Adım D2 — Eşya altyapısı ✅ (görünmez altyapı; yerleştirme D3'te)
Masalar artık sahneye elle konmuş sabit objeler değil: **veriden oluşturulan, konumu/açısı kaydedilen eşyalar.** Oyuncu açısından bir şey değişmedi (masalar aynı yerde) — bu adım D3'teki yerleştirme modunun zemini.

> ⚠️ **Kullanıcı geri bildirimi (2026-10-17):** test için yazılan `Kan Bagi > Test > Masalari Kaydir` (bütün boş masaları birden +0.5 m kaydırıp 30° döndüren düğme) kullanıcı tarafından **yerleştirme sistemi sanıldı** ve haklı olarak "saçma" bulundu. Ders: **görünmez bir altyapı adımında, oyuncunun göreceği şeyin hangi adımda geleceğini baştan açıkça söyle;** test aracını gerçek özellikmiş gibi sunma. Bu düğme **D3'te silinecek.**

**Parçalar:**
- **Veri** (`KanBagi.Data`): `FurnitureData` (displayName, `FurnitureCategory` Table/Station/Decor, prefab, price — fiyat D4'te), `FurnitureDatabase` (asset adı → tanım; **yeni eşya buraya eklenmeli**), `FurnitureLayout` (`FurnitureLayoutEntry`: eşya + konum + **yaw**).
  Asset'ler: `MasaEsya`, `EsyaVeritabani`, `VarsayilanDuzen` (Masa1 (−2.5, 0.4, −1), Masa2 (2.5, 0.4, −1)).
- **Oyun içi** (yeni klasör `Scripts/Furniture/`, **`KanBagi.Furniture`**):
  - `PlacedFurniture` — eşya kökünde "ben hangi eşyayım" (`Data`, `Yaw`).
  - `FurnitureManager` (**`[DefaultExecutionOrder(50)]`**, Managers, `ISaveable` anahtar **`furniture`**): `Spawn`, `Move`, `Remove` (tek geçiş noktası = co-op RPC sınırı), `OnLayoutChanged`. Kayıt yoksa `Start`'ta varsayılan düzeni kurar (SaveManager'dan (100) önce). Kayıt: eşya **asset adı** + konum + yaw, ayrıca `granted` (eşyasını vermiş upgrade adları). Yüklemede düzen **tamamen** kayıttakiyle değişir. Silerken önce `SetActive(false)` (masa `TableManager`'dan hemen çıksın), sonra `Destroy`.
  - `NavMeshRuntimeBaker` (Managers): `OnLayoutChanged` ve **`OnUpgradePurchased`** (kilitli makine açılınca) → **bir sonraki karede bir kez** `NavMeshSurface.BuildNavMesh()`. Aynı karedeki istekler birleşir. Yeni veri bellekte oluşur, diskteki asset'e dokunulmaz. **Editördeki `NavMesh'i Yeniden Bake Et` artık zorunlu değil** (oyun başında zaten bake ediliyor), ama editör görünümü için kullanılabilir.
  - `UpgradeVisual` (masa prefab'ında): upgrade alınınca **kendi** okey/iskambil/domino parçasını açar; sonradan oluşturulan masada `Start`'ta `IsPurchased` ile hemen uygular. Sahnedeki parça açıcı `UpgradeUnlocker`'ların yerini aldı (D8'in temeli).
  - `UpgradeFurnitureGrant` (Managers): "Yeni Masa" alınınca Masa3'ün eski yerine (0, 0.4, −2) **bir kez** masa koyar; `FurnitureManager.MarkGranted(upgrade adı)`. **Yükleme sırası belirsizliğine dayanıklı:** upgrade önce yüklenirse verilen masa, ardından gelen eşya kaydıyla değiştirilir; eşya önce yüklenirse `granted` sayesinde atlanır. Eski (eşya bölümü olmayan) kayıtta varsayılan düzen + "Yeni Masa" alınmışsa hibe. **Geçici:** D3/D4'te masa elde hayalet olarak gelecek.
- **Editor:** `Kan Bagi > Esya Altyapisini Kur` (`FurnitureSetupBuilder`, **bir kez**): Masa1'den `Masa` prefab'ı (Table + BoxCollider + NavMeshModifier "Not Walkable" + `PlacedFurniture` + `UpgradeVisual` + parçalar `OkeyTakimi`/`IskambilDestesi`/`DominoTakimi`, başta kapalı), veri asset'leri, Managers bileşenleri; **sahnedeki 3 masa ve 4 eski `UpgradeUnlocker` (YeniMasa, Okey, İskambil, Domino) silindi.** Hangi upgrade'in ne açtığını asset adından değil **mevcut açıcıların hedeflerinden** çıkardı. Kalan açıcılar: Kahve, Oralet, Kumarhane, Sahlep.
- `TableGamesBuilder` (`Masa Oyunlarini Kur`): sahnede masa yoksa "artık gerekmiyor, parçalar `Masa` prefab'ında" der.
- ⚠️ Editörde `GetComponent<T>() ?? ...` kullanılmaz: eksik bileşen "sahte null" döner, `??` yakalamaz → `TryGetComponent`.

**Test:** yeni oyunda masalar eski yerlerinde, servis/tepsi çalışıyor; "Yeni Masa" Masa3'ü getiriyor, parçalar üç masada görünüyor; test düğmesiyle kaydırılan masaların etrafından müşteriler yürüyor; kayıttan kaydırılmış düzen geri geliyor, Masa3 çoğalmıyor; makineler açılınca içinden geçilmiyor. Kullanıcı onayladı ("dediklerin çalışıyor").

### Adım D3 — Yerleştirme modu ✅
**Oyuncu ne görür:** boş bir masaya bakıp **F** → sadece o masa kalkar, **yarı saydam hayalet** olur ve bakılan zemin noktasını takip eder (en fazla 5 m, **0.1 m ızgara**). **Tekerlek 5°**, **Shift + tekerlek 45°** döndürür. **Yeşil** = konabilir, **kırmızı** = konamaz. **Sol tık** bırakır (NavMesh + kayıt güncellenir), **sağ tık** iptal (eski yerine döner). Altta "Sol tık: Koy | Tekerlek: Döndür (Shift: 45) | Sağ tık: İptal". **"Yeni Masa"** PC'den alınınca ekran kapanır, masa **elde hayalet** gelir (iptal edilirse ilk belirdiği yerde — eski Masa3 noktası — kalır).

**Kurallar:** tek seferde tek eşya; **dolu masa taşınmaz** (oturan/yürüyen müşteri — `IsFree` — veya üstünde bardak → "Masa dolu, taşınamaz"); elde eşya varken başlamaz ("Önce elindekini bırak"); oyuncu kilitlenirse (gün sonu paneli, PC) taşıma iptal.

**Geçerlilik (kırmızı sebepleri):** ışın dükkan zeminine (**`NavMeshSurface`'li obje ve çocukları** — sokak sayılmaz) çarpmıyor; eşyanın kök `BoxCollider`'ı (%95 küçültülmüş) zemin dışında **herhangi bir collider'a** değiyor (eşya, makine, müşteri, oyuncu, bardak); **masanın oturma noktası** zeminde değil veya orada oturan müşteri boyunda (r 0.3, 1.6 m) bir kapsül boş değil.

**Parçalar:**
- `FurniturePlacer` (`KanBagi.Furniture`, **Player üzerinde, singleton değil**): girdi, hayalet, ızgara, döndürme, geçerlilik. Taşırken eşyanın collider'ları kapalı (ışın kendine çarpmasın), `Table` bileşeni kapalı (**`OnDisable` → `TableManager`'dan çıkar**, müşteri ayıramaz), `PlayerInteractor` kapalı (E başka şeye dokunmasın, ipucu gizlenir). Malzemeler geçici olarak runtime üretilen **URP Lit şeffaf** yeşil/kırmızı malzemeyle değişir, bitince geri yüklenir. Pivot yüksekliği collider `bounds`'undan ölçülür. **Onayda tek geçiş noktası `FurnitureManager.Move`.** `BeginPlacingNew(PlacedFurniture)` — dışarıdan yeni eşyayı ele verir. `OnPlacingChanged`, `OnMessage`.
- `PlacementHintUI` (`KanBagi.UI`): ipucu + 2 sn'lik mesajlar.
- `UpgradeFurnitureGrant`: eşyayı `position`'da oluşturur, **PC'yi kullanan oyuncu** (`ComputerStation.ScreenUser`) varsa ekranı kapatıp onun `FurniturePlacer`'ına verir.
- `ComputerStation.ScreenUser` (yeni özellik).
- **Girdi** (`InputSystem_Actions.inputactions`, Player haritası): yeni **`MoveFurniture` (F)**, **`RotateFurniture` (`<Mouse>/scroll/y`, Value/Axis)**, **`CancelPlacement` (sağ tık)**. Koy = mevcut **`Attack`** (sol tık; başka yerde kullanılmıyor), hızlı döndürme = mevcut **`Sprint`** (Shift) basılı mı.
- `Kan Bagi > Yerlestirme Modunu Kur` (`PlacementModeBuilder`): Player'a `FurniturePlacer`, Canvas'a `YerlestirmeIpucu` (alt orta, Pos Y 140, panellerin altında) + `PlacementHintUI`. Tekrar çalıştırılabilir.
- **Silindi:** `FurnitureTestMenu` (`Masalari Kaydir`).
- Şimdilik taşınabilir tek eşya **masa** (`PlacedFurniture` sadece masalarda); diğerleri D5'te.

**Test:** F ile tek masa kalkıyor, hayalet bakışı takip ediyor, tekerlek/Shift döndürüyor, kırmızı/yeşil doğru, sol tık bırakıyor, sağ tık geri alıyor; müşteriler yeni yerin etrafından yürüyüp oturuyor; dolu masa taşınmıyor; "Yeni Masa" elde hayalet geliyor; kayıttan düzen geri geliyor. Kullanıcı onayladı.

**Değişecek mevcut parçalar (bilerek):** sabit eşyalar + `UpgradeUnlocker`, editörde NavMesh bake (`NavMesh'i Yeniden Bake Et`), `Icecek Istasyonlarini Kur` / `Masa Oyunlarini Kur` builder'ları, tek kişilik `Table`, tek eşya tutan eller, dükkan geneli masa oyunları. **Değişmeyenler:** müşteri profilleri, sipariş/ikinci tur, menü, stok/kurye, ekonomi, itibar, gün döngüsü, kayıt altyapısı, kumar/mafya/polis, fısıltılar, hikaye, co-op etkileşim katmanı.

---

## 7. SAHNE KURULUMU (`Assets/Scenes/SampleScene.unity`)

| Obje | Konum | Üzerindeki script'ler |
|---|---|---|
| `Zemin` (Plane) | (0,0,0) | **NavMeshSurface** — `Kan Bagi > NavMesh'i Yeniden Bake Et` ile bake edilir (elle Bake'e basma: kilitli eşyaların deliği açılmaz). Tüm eşyalarda "Not Walkable" `NavMeshModifier` var |
| `Player` (Empty) | (0, 1.1, 0) | CharacterController, PlayerController, PlayerInteractor, PlayerHands, PlayerLock, **FurniturePlacer** |
| └ `Main Camera` | (0, 0.6, 0) | — (child) |
| `Managers` (Empty) | — | EconomyManager, ReputationManager, TableManager, InventoryManager, PurchaseManager, DeliveryManager, UpgradeManager, MenuManager, CustomerModifiers, StationSpeedBoost, CustomerPatienceBoost, DayCycleManager, DailyStatsManager, SaveManager, PropertyManager, SuspicionManager, MafiaManager, PoliceManager, **BalanceLogger**, **4 × UpgradeUnlocker** (KahveMakinesi→KahveMakinesi, Oralet→OraletSemaveri, Kumarhane→KumarMasasi, SahlepMakinesi→SahlepMakinesi — masa/parça açıcıları D2'de kalktı), **2 × TableGameBoost** (İskambil 0.3, Domino 0.2), **FurnitureManager, NavMeshRuntimeBaker, UpgradeFurnitureGrant** (YeniMasa → masa (0, 0.4, −2)) |
| `EventSystem` | — | **Input System UI Input Module** (butonlar bunsuz çalışmaz) |
| `Canvas` | — | InteractionPromptUI, HeldItemUI, ProgressBarUI, **ComputerUI** |
| ├ `MoneyText` | sol üst (Pos Y -20) | MoneyUI |
| ├ `ReputationText` | Pos Y -55 | ReputationUI |
| ├ `StockText` | Pos Y -90 | StockUI (tracked: CayYapragi, KahveCekirdegi, OraletTozu, SahlepTozu) |
| ├ `PromptText` | orta-alt | — |
| ├ `HeldItemText` | sol alt | — |
| ├ `BildirimText` | üst-orta | DeliveryNotificationUI |
| ├ `SaatText` | sağ üst | ClockUI |
| ├ `PolisUyariText` | üst-orta (Pos Y -90) | PoliceNotificationUI |
| ├ `GunlukOzetPaneli` (**Canvas'ın son çocuğu**) | tam ekran | DailyReportUI (kök hep aktif) |
| &nbsp;&nbsp;└ `Icerik` → `Panel` (560×460, VLG) | orta | 7 satır + `YeniGunButton` |
| ├ `FisiltiText` (panellerden önce) | üst orta (Pos Y −70) | WhisperUI — `Kan Bagi > Fisilti Satirini Olustur` kurar |
| ├ `YerlestirmeIpucu` (panellerden önce) | alt orta (Pos Y 140) | PlacementHintUI — `Kan Bagi > Yerlestirme Modunu Kur` kurar |
| ├ `HikayeGirisPaneli` (prefab, **Canvas'ın son çocuğu**) | tam ekran | StoryIntroUI (kök hep aktif) — `Kan Bagi > Hikaye Giris Panelini Olustur` kurar |
| ├ `ProgressBar` → `Dolum` (Image/Filled) + `ProgressLabel` | orta-alt | — |
| └ `BilgisayarPaneli` (700×450, **Vertical Layout Group**, sahnede PASİF) | orta | TabGroupUI |
| &nbsp;&nbsp;├ `UstBar` (HLG, pref. height 44) → `Baslik`, `KayitDurumText`, `KasaText`, `KaydetButton`, `KapatButton` | — | — |
| &nbsp;&nbsp;├ `SekmeBar` (HLG, pref. height 50) → `MalzemeSekmeButton`, `GelistirmeSekmeButton`, `EmlakSekmeButton` | — | butonlarda Transition = None |
| &nbsp;&nbsp;├ `MalzemeSayfasi` (VLG, flexible height 1) | — | ComputerUI'ın `entryContainer`'ı |
| &nbsp;&nbsp;├ `UpgradeSayfasi` (VLG, flexible height 1) | — | UpgradeListUI |
| &nbsp;&nbsp;└ `EmlakSayfasi` (VLG, flexible height 1) | — | PropertyListUI |
| `BardakRafi` (Cube) | (-2, 0.5, 2) | GlassShelf (stok 10, prefab: CayBardagi) |
| `TepsiYeri1` / `TepsiYeri2` (+ üstlerinde 2 `Tepsi` prefab'ı) | rafın üstü (−2.25 / −1.75, 0.8, 2) | TraySpot — `Tepsiyi Kur` kurar |
| `CayOcagi` (Cube) | (0, 0.5, 3) | TeaStove (Cay + CayYapragi × 1) |
| `KahveMakinesi` (Cube) — **başta pasif** | (-1, 0.5, 3) | TeaStove (Kahve + KahveCekirdegi × 1) |
| `OraletSemaveri` (Cube) — **başta pasif** | (1, 0.5, 3) | TeaStove (Oralet + OraletTozu × 1) |
| `Lavabo` (Cube) | (2, 0.5, 3) | Sink (washDuration 2.5) |
| `SahlepMakinesi` (Cube, 1×1×0.8, krem) — **başta pasif** | (3.5, 0.5, 3) | TeaStove (Sahlep + SahlepTozu × 1) — `Icecek Istasyonlarini Kur` kurar |
| ~~`Masa1/2/3`~~ — **D2'den beri sahnede masa YOK** | — | Masalar oyun başında `FurnitureManager` tarafından `Masa` prefab'ından oluşturulur (varsayılan düzen / kayıt). Prefab'da `OkeyTakimi` (yerel 0.3, 0.6, 0.3), `IskambilDestesi` (−0.3, 0.52, 0.3), `DominoTakimi` (0.3, 0.53, −0.3) — collider yok, `UpgradeVisual` açar |
| `GirisNoktasi` (Empty) | (0, 0, -4.5) | — (spawn + exit) |
| `Spawner` (Empty) | — | CustomerSpawner (**profiles**: EmekliDayi, Ogrenci, Esnaf, Kabadayi — `Musteri Profillerini Bagla` bağlar) |
| `YonetimPC` (Cube) | (2, 0.5, 2) | ComputerStation (catalog: MalzemeKatalogu) |
| `TeslimatNoktasi` (Empty) | (2, 0, -3.5) | — (kapının yanı, koliler buraya düşer) |
| `Mahalle` (Empty) | — | — |
| ├ `Sokak` (Plane) | (0, 0, -10), scale (1.5,1,1) | — |
| ├ `BakkalBinasi` (Cube, scale 3) | (-4, 1.5, -9) | PropertyBuilding (BakkalMulk) + tabela child'ı |
| ├ `TerziBinasi` (Cube, scale 3) | (0, 1.5, -12.5) | PropertyBuilding (TerziMulk) |
| └ `KirathaneBinasi` (Cube, scale 3) | (4, 1.5, -9) | PropertyBuilding (KirathaneMulk) |
| `Depo` (Cube) | (-2, 0.5, 3.5) | StorageRack |
| `KumarMasasi` (Cube) — **başta pasif** | (-4, 0.4, 3.5) | GamblingTable + **GamblerSpawner** + tabela child'ı (Gizli Kumarhane upgrade'i açar) |
| `MafyaNoktasi` (Empty) | (1.5, 0.9, -4) | — (tahsildar burada belirir, y=0.9 kapsül yere bassın diye) |
| `PolisNoktasi` (Empty) | (-1.5, 0.9, -4) | — (memur burada belirir) |

**Prefab'lar:**
- `CayBardagi` (Cylinder 0.08/0.05/0.08 + child `Sivi` + HoldableItem + TeaGlass)
- `Musteri` (Capsule + NavMeshAgent + Customer + OrderBubbleUI + child `SiparisBalonu` (3D TMP + Billboard)) — **Desired Order BOŞ** (menüden rastgele seçilir), Drink Duration 6, Patience 25, itibar servis +1 / kızgın gidiş **−2** (10c-1'den beri sabır/içme süresi profil çarpanıyla, kızgın gidiş cezası **profilden** gelir; prefab değerleri temel ve profilsiz müşteri için)
- `UrunSatiri`, `UpgradeSatiri`, `EmlakSatiri` — **elle kurulmaz**, `Kan Bagi > Satir Prefablarini Olustur` menüsü üretir
- `GunlukOzetPaneli` — **elle kurulmaz**, `Kan Bagi > Gunluk Ozet Panelini Olustur` menüsü üretir
- `EmlakTabelasi` — **elle kurulmaz/yerleştirilmez**, `Kan Bagi > Emlak Tabelasi Prefabini Olustur` + `Kan Bagi > Tabelalari Binalara Yerlestir`
- `KumarTabelasi` — **elle kurulmaz**, `Kan Bagi > Kumar Tabelasini Olustur ve Yerlestir`
- `Tahsildar` — **elle kurulmaz, sahneye elle konmaz**, `Kan Bagi > Tahsildar Prefabini Olustur`; MafiaManager runtime'da üretir/yok eder
- `PolisMemuru` — aynı şekilde `Kan Bagi > Polis Prefabini Olustur`; PoliceManager runtime'da üretir/yok eder
- `KumarMasasi > Ortu` — `Kan Bagi > Kumar Ortusunu Olustur ve Bagla` menüsü üretip bağlar
- `Koli` (Cube 0.35/0.3/0.35 + **Rigidbody** + HoldableItem + DeliveryBox) — Rigidbody olmazsa koli havada asılı kalır
- `Tepsi` — **elle kurulmaz**, `Kan Bagi > Tepsiyi Kur` üretir (kök ölçeksiz, HoldableItem + Tray)
- `Kumarbaz` — **elle kurulmaz, sahneye elle konmaz**, `Kan Bagi > Kumarhaneyi Kur`; GamblerSpawner gece runtime'da üretir
- `HikayeGirisPaneli` — **elle kurulmaz**, `Kan Bagi > Hikaye Giris Panelini Olustur` üretir ve Canvas'a yerleştirir

**ScriptableObject'ler:**
| ItemData | Tür | SellPrice | PurchaseCost | PrepareDuration |
|---|---|---|---|---|
| `Cay` | Beverage | 15 | 3 | 3 |
| `Kahve` | Beverage | 25 | 0 | 5 |
| `Oralet` | Beverage | 18 | 0 | 2 |
| `Sahlep` | Beverage | 30 | 0 | 4 |
| `CayYapragi` | Consumable | 0 | 2 | 0 |
| `KahveCekirdegi` | Consumable | 0 | 4 | 0 |
| `OraletTozu` | Consumable | 0 | 3 | 0 |
| `SahlepTozu` | Consumable | 0 | 5 | 0 |

- `MalzemeKatalogu` (ShopCatalog) — CayYapragi ×10 (20 TL), KahveCekirdegi ×10 (40 TL, kilitli), OraletTozu ×10 (30 TL, kilitli), **SahlepTozu ×10 (50 TL, kilitli)**
- `GelistirmeKatalogu` (UpgradeCatalog) — YeniMasaUpgrade (100), KahveMakinesiUpgrade (200), OraletUpgrade (100), SanayiOcagiUpgrade (250), OkeyTakimiUpgrade (150), GizliKumarhaneUpgrade (1500 + **itibar 180**), **IskambilUpgrade (200 + itibar 40), DominoUpgrade (350 + itibar 100), SahlepMakinesiUpgrade (300 + itibar 80)**
- `UrunVeritabani` (ItemDatabase) — 8 ItemData'nın tamamı (kayıt sistemi için, **yeni ürün eklenince buraya da eklenmeli** — `Icecek Istasyonlarini Kur` bunu otomatik yapar)
- `BakkalMulk` (800 TL / 60 / itibar **60**), `TerziMulk` (1200 / 90 / **140**), `KirathaneMulk` (2000 / 160 / **240**) — 9a-3'te güncellendi
- `EmlakKatalogu` (PropertyCatalog) — üç mülkün tamamı (**kayıt eşlemesi buradan yapılır, boş bırakılamaz**)
- `GirisHikayesi` (StoryData) — giriş ekranının 5 sayfası ve buton yazıları
- `MahalleFisiltilari` (WhisperData) — fısıltı metinleri, kumarhane upgrade referansı, itibar barajları (60/140/240)
- `ArayuzMetinleri` (TextPreset) — sahne/prefab metin alanlarının yeni değerleri; `Kan Bagi > Metinleri Uygula` yazar
- `MusteriProfilleri/` (CustomerProfile) — `EmekliDayi`, `Ogrenci`, `Esnaf`, `Kabadayi` (değerler bölüm 6.8, 10c-1)

> ✅ **Sahne GERÇEK değerlerde** (Adım 9a-1). Hızlı test için `Kan Bagi > Denge > Test Degerlerini Uygula`, geri dönmek için `Gercek Degerleri Uygula`. Mod değiştirdikten sonra sahneyi kaydet; sıfırdan test için `Kan Bagi > Kaydi Sil`.

---

## 8. ŞU AN NEREDE KALDIK

**Adım 10c-3 bitti ve test edildi.** Sahlep (30 TL) Sahlep Makinesi upgrade'iyle (300 TL, itibar 80) açılıyor; makine lavabonun sağında beliriyor, toz sipariş edilip sahlep yapılıyor, Dayı/Öğrenci/Kabadayı istiyor, kayıttan geri geliyor. Yeni içecekler artık `Icecek Istasyonlarini Kur` tablosuna bir satırla ekleniyor. **10c-4 (bahçe) iptal edildi** — kullanıcının "kendi dükkanını kur" vizyonu üzerine yeni **Dükkan Düzenleme** bölümü açıldı (bölüm 6.9).

**Adım D1 bitti ve test edildi — DÜKKAN DÜZENLEME BAŞLADI.** Tepsi 8 bardak taşıyor: raftan her E bir bardak, ocağa bırakınca her E bir bardak daha dolduruyor, masada tek basışta kirliyi toplayıp servis ediyor, lavaboda toplu (bardak başı 1.5 sn) yıkanıyor; tepsiler rafın üstünde duruyor.

**Adım D2 bitti ve test edildi (altyapı).** Masalar veriden oluşturuluyor, konum/açıları kayda giriyor, NavMesh oyun içinde güncelleniyor, "Yeni Masa" hibe ile geliyor, masa oyunu parçalarını her masa kendisi açıyor. Oyuncuya görünür bir değişiklik yok.

**Adım D3 bitti ve test edildi.** Masalar F ile tek tek kalkıp elde hayalet oluyor, 0.1 m adımla bakılan yere gidiyor, tekerlekle 5° (Shift 45°) dönüyor, yeşil/kırmızı geçerlilik çalışıyor, sol tık bırakıyor, sağ tık iptal ediyor; "Yeni Masa" PC'den alınınca elde hayalet geliyor. **Sıradaki iş: D4 — Eşya satın alma** (PC'de eşya kataloğu; masa birden fazla alınabilir, alınca elde hayalet; "Yeni Masa" upgrade'i ve hibe kalkar; sarf malzemesi kuryeyle kalır — plan ayrıca onaylanacak).

**Adım 10c-2 bitti ve test edildi.** İskambil (200 TL, itibar 40, +%30) ve Domino (350 TL, itibar 100, +%20) masalarda beliriyor; müşteriler içtikten sonra oyun meraklarına göre bir tur daha sipariş veriyor, ciro iki satışı da sayıyor, ikinci turda bekletmenin cezası yok.

**Adım 10c-1 bitti ve test edildi.** Müşteriler artık dört tipte (Emekli Dayı, Öğrenci, Esnaf, Kabadayı): renkleri, sabırları, içecek zevkleri ve bahşişleri farklı; Esnaf itibar 60'ta, Kabadayı 140'ta gelmeye başlıyor; bahşiş ciroya giriyor.

**Adım 10a-2b bitti ve test edildi — 10a (HİKAYE VE TON) KAPANDI.** Polis, teslimat, tahsildar, kumar, mülk ve gün sonu metinleri intikam tonunda; metinler `ArayuzMetinleri.asset`'te, `Kan Bagi > Metinleri Uygula` ile sahne ve prefab'lara yazılıyor.

**Adım 10a-2a bitti ve test edildi.** Mahalle fısıltıları çalışıyor: ilk sabah, tahsildar geliş/ödeme/ret, mülk ve kumarhane alımı, itibar barajlarında üst ortada intikam tonunda bir satır beliriyor; kayıttan devam edince eski olaylar tekrar fısıldanmıyor.

**Adım 10a-1 bitti ve test edildi.** Yeni oyunda siyah ekranda 5 sayfalık intikam hikayesi anlatılıyor; okunurken saat duruyor ve oyuncu kilitli, "Ocağı Yak" ile oyun başlıyor, "Atla" çalışıyor, kayıtlı oyunda ekran gelmiyor.

**Adım 9a-2 bitti ve test edildi — 9. BÖLÜM (DENGE + CO-OP HAZIRLIĞI) KAPANDI.** Gece kumarhanesine NPC kumarbazlar geliyor, masanın etrafından dolaşıp oturuyor, el oynuyor; kasa her elden 25 TL pay alıyor, şüphe artıyor, tabela ve günlük özet doğru. Kumarbazların masanın üstünden yürüme bug'ı NavMesh'in kilitli eşyalarla yeniden bake edilmesiyle çözüldü. **Sıradaki iş: 10a — hikaye girişi** (plan ayrıca onaylanacak).

**Adım 9b bitti ve test edildi.** Etkileşimler artık etkileşimi yapan oyuncuyu (`PlayerContext`) taşıyor; `PlayerHands`/`PlayerLock` singleton değil, bilgisayar sadece açanı kilitliyor, gün sonu herkesi kilitliyor. Davranış birebir aynı. **Sıradaki iş: 9a-2 — kumarhane işletmesi** (plan ayrıca onaylanacak).

**Adım 9a-3 bitti ve test edildi.** Polis cezası kasada en az 150 TL bırakıyor (iflas kilidi kapandı). İtibar barajları paranın yetiştiği günlerin arkasına çekildi (60 / 140 / 240, kumarhane 180), mafya itibar 120 + kasa 1000 ile geliyor ve 300 TL haraç istiyor, kızgın müşteri −2 itibar götürüyor. **Sıradaki iş: 9b — co-op hazırlığı (etkileşim katmanı).**

**Adım 9a-1 bitti ve test edildi.** Sahne gerçek değerlere döndü (250 TL, itibar 0, 6 dk'lık gün, gece hızlı), kopya DayCycleManager silindi, `BalanceLogger` her oturumu/günü/kilometre taşını `DengeLog/denge_log.csv`'ye yazıyor. **Sıradaki iş:** kullanıcı sıfırdan 6–8 gün oynayacak, Claude CSV'yi okuyup analiz edecek.

**Adım 8c bitti ve test edildi.** Şüphe biriktikçe polis baskın yapıyor: uyarı geliyor, oyuncunun masayı örtmek için ~20 saniyesi oluyor, örtmezse para cezası + itibar kaybı, örterse temiz geçip şüphe düşüyor. Faz 4'ün üç ayağı (kumarhane, mafya, polis) birbirine bağlandı.

**Adım 8b bitti ve test edildi.** Mafya itibar 30 + servet 2000 barajını aşınca haberdar oluyor, birkaç gün sonra tahsildar kapıya geliyor; ödenince para düşüyor ve adam gidiyor, ödenmeyince ertesi gün itibar/şüphe cezası uygulanıp haraç ×1.5 büyüyor. Günlük özette "Harac" satırı doğru, kayıt/yükleme sorunsuz.

**Adım 8a bitti ve test edildi — FAZ 4 BAŞLADI.** Gizli kumarhane itibar 50 + 1500 TL ile açılıyor, sadece geceleri oynanıyor, tabela sonucu ve gecelik hesabı gösteriyor, günlük özette "Kumar" satırı doğru (bahisler giderde çift sayılmıyor), şüphe birikiyor ve kaydediliyor.

**Adım 7b bitti ve test edildi — FAZ 3 KAPANDI.** Dükkandan çıkıp sokağa yürünüyor, binaların duvarındaki SATILIK tabelaları bilgileri gösteriyor, mülk yerinde E ile satın alınıyor, alınınca tabela kayboluyor; PC ve sokak arayüzleri senkron.

**Adım 7a bitti ve test edildi — FAZ 3 BAŞLADI.** PC'de EMLAK sekmesinden mülk alınıyor, itibar barajı çalışıyor, kira ertesi günden itibaren her gün kasaya yatıyor, günlük özette "Kira geliri" görünüyor ve hepsi kayıttan doğru geri geliyor.

**Adım 6c bitti ve test edildi — FAZ 2 ALTYAPISI TAMAM.** Para, itibar, stok, alınan upgrade'ler ve gün numarası diske kaydediliyor; oyun açılınca kaldığı yerden devam ediyor, kilitli objeler ve menü doğru şekilde geri geliyor.

**Adım 6b bitti ve test edildi.** Gün 24:00'te bitince özet paneli açılıyor, oyuncu kilitleniyor; servis edilen/kaçırılan müşteri, ciro, gider, net kâr ve itibar değişimi doğru görünüyor; "Yeni Gun" ile yeni gün başlıyor ve sayaçlar sıfırlanıyor.

**Adım 6a bitti ve test edildi.** Gün 00:00'dan 24:00'a kesintisiz akıyor, gece hızlı geçiyor, 21:00'den sonra yeni müşteri gelmiyor, 22:00'de içerideki herkes kalkıp çıkıyor (yarım kalan bardak masada kirli kalıyor), 24:00'te yeni gün başlıyor.

**Adım 5f bitti ve test edildi.** Sanayi tipi ocak tüm içecek istasyonlarını hızlandırıyor (çay 3→1.8 sn, kahve 5→3 sn), okey takımı hem masalarda beliriyor hem müşteri sabrını 25→37.5 sn'ye çıkarıyor. Okey kutuları servis ışınını engellemiyor.

**Adım 5e bitti ve test edildi — FAZ 2 KAPANDI.** Müşteriler menüden rastgele sipariş veriyor (çay/kahve/oralet), her müşteri **sadece kendi istediğini** kabul ediyor, makinesi olmayan içecek menüye girmiyor, malzemesi kilitli kalıyor.

**Adım 5d bitti ve test edildi.** PC arayüzü sıfırdan, Layout Group tabanlı kuruldu — hizalama sorunu yok. `MALZEMELER` / `GELISTIRME` sekmeleri çalışıyor. Yeni Masa 100 TL'ye alınıyor, buton `Alindi` oluyor, `Masa3` sahnede beliriyor ve müşteri gelip oturuyor.

**Adım 5c bitti ve test edildi.** Sipariş artık ışınlanmıyor: PC'den alınıyor → "yolda" bildirimi → süre sonunda kapının yanına koli düşüyor → oyuncu koliyi alıp `Depo`'ya götürüp boşaltıyor → stok artıyor.

**Adım 5b bitti ve test edildi.** `YonetimPC`'ye E ile bakılınca ekran açılıyor, imleç serbest kalıyor, oyuncu kilitleniyor; 20 TL'ye 10 ölçek çay yaprağı alınıyor, stok ve para anında güncelleniyor, para yetmeyince Satın Al butonu pasifleşiyor, ESC/Kapat ile çıkılıyor.

**Adım 5a bitti ve test edildi.** Çay artık bedava değil: her bardak 1 ölçek `CayYapragi` harcıyor, sol üstte stok görünüyor, stok bitince ocak çalışmıyor ve **bardak oyuncunun elinde kalıyor.**

**Adım 4b bitti ve test edildi — FAZ 1 TAMAMEN KAPANDI.** Oyuncu çayı hazırlayıp müşteriye servis ediyor, para ve itibar kazanıyor, müşteri içip gidiyor, masada kalan kirli bardağı oyuncu lavaboya götürüp rafa iade ediyor. Bekletilen müşteri itibar düşürüyor.

Şu anda çalışan tam döngü:
```
BardakRafi ──E──> [Elde bos bardak] ──E──> CayOcagi (3sn) ──E──> [Dolu cay]
     ▲                                                                │
     │                                                                E (Masa)
     │                                                                ▼
     │                                              +15 TL, +1 Itibar / musteri icer (6sn)
     │                                                                │
     └──── Lavabo (2.5sn) <──── [Masada kirli bardak] <───────────────┘

Musteri: giris → masaya yuru → otur → "Cay" balonu → SERVIS → icer → cikar
         (servis edilmezse: sabir biter → -1 itibar → cikar)
```

**Faz 2 bitti.** Tüm döngü çalışıyor:
```
PC'den siparis → kurye koliyi kapiya birakir → depoya tasi → stok
     ↓                                                        ↓
  para                                    Raf → Bardak → Ocak/Makine → Dolu icecek
     ↑                                                        ↓
  +TL, +itibar ←──── MASA (servis) ←───────────────────────────┘
     ↓                     ↓
  GELISTIRME        kirli bardak → Lavabo → Raf
  (masa, makine)
```
Faz 2'nin tamamı bitti: stok, PC, kurye, upgrade'ler (obje açma **ve** stat iyileştirme), menü çeşitliliği.

**Eksik olan iki temel altyapı:**
1. **Kalıcılık yok** — oyunu kapatınca para, stok ve upgrade'ler sıfırlanıyor.
2. **Zaman kavramı yok** — gün/gece yok. Faz 3'ün "pasif gelir"i ve Faz 4'ün "haraç"ı ikisi de periyodik bir zaman birimi ister.

---

**Master prompt'taki dört fazın tamamı iskelet olarak ayakta.** Oyun baştan sona oynanabiliyor: çay sat → büyü → mahalleyi al → geceleri kumar → mafya ve polisle başa çık.

Bundan sonrası **cila ve denge**. Öncelik sırasıyla:

---

## 9. DENGE (9A), CO-OP HAZIRLIĞI (9B) VE SIRADAKİ ADIM — 9A-2

> 9a-1, 9a-3, 9b ve 9a-2 bitti (ayrıntı bölüm 6.7). Aşağısı tarihçe olarak duruyor; **sıradaki iş 10a** (hikaye girişi, bölüm 10).

**Amaç (9a):** Oyunu baştan sona oynanabilir hale getirmek. **Ölçüm atlandığı için** aşağıdaki kağıt üstü tahminler üzerinden gidildi.

**En kritik iki sorun:**
1. **İflas kilidi** — polis cezası parasızken kasadaki her şeyi alıyor; stok da bittiyse çay yapılamaz, para kazanılamaz, oyun kilitlenir. Çözüm gerekiyor (asgari kasa, ceza üst sınırı veya borç mekaniği).
2. **İtibar barajları fiilen kilit değil** — itibar paradan hızlı büyüdüğü için 10/20/30/50 barajları 1-4. günde aşılıyor; kumarhane ve mafya erken açılıyor.

**Kullanıcı kararları (onaylandı):**
- **Kumar:** Hem **kumarhane işletilecek** (gece NPC kumarbazlar oynar, kasa pay alır, şüphe artar) **hem de** oyuncu isterse masada kendisi oynayabilecek (mevcut mod kalır).
- **Müşteri sıklığı:** kod varsayılanı (ilk 3 sn, aralık 6–14 sn).

**Alt adımlar (sıra 2026-10-10'da güncellendi — bkz. bölüm 10 tablosu):**
1. ✅ **9a-1** — Gerçek değerler + `BalancePresetApplier` + `BalanceLogger`.
2. ❌ **Ölçüm — ATLANDI.** Kullanıcı 6–8 günlük ölçümlü oynamayı istemedi ("üşendim"). `BalanceLogger` yerinde duruyor, ileride istenirse kullanılabilir. **Denge düzeltmesi aşağıdaki kağıt tahminler üzerinden yapılacak.**
3. ✅ **9a-3 — Denge düzeltmeleri:** iflas koruması (asgari kasa 150) + itibar barajları, mafya, haraç. Ayrıntı bölüm 6.7.
4. ✅ **9b — Co-op hazırlığı: etkileşim katmanı refactor.** Ayrıntı bölüm 6.7.
5. ✅ **9a-2 — Kumarhane işletmesi** (NPC'ler oynar, kasa pay alır). Ayrıntı bölüm 6.7.

**Kağıt üstü tahminler (ölçümle doğrulanacak):**
- Tek oyuncu + 2 masa ≈ günde 12–15 servis ≈ **+190 TL** ve **+13 itibar**.
- **İtibar paradan hızlı büyüyor** → 10/20/30/50 barajları 1–4. günde aşılır, fiilen kilit değiller.
- Erken upgrade'lerin toplamı 800 TL → 4–5. günde hepsi alınır.
- **Kumar EV negatif:** winChance 0.45 × payout 2 = el başına **−10 TL** (oyuncu kendi oynarken).
- Haraç 500 TL / 3 gün ≈ günlük çay gelirinin tamamı.
- **İflas kilidi riski:** polis cezası parasızken kasadaki her şeyi alıyor; stok da 0 ise çay yapılamaz → para kazanılamaz → oyun kilitlenir.

**Sonraki adımların tam sırası bölüm 10'daki tabloda.**

### 9b — Co-op hazırlığı: etkileşim katmanı ✅ (BİTTİ — aşağısı ilk plan, uygulanan hali bölüm 6.7)

**Amaç:** Her etkileşimin "hangi oyuncu yaptı" bilgisini taşıması. Co-op'un tek gerçek engeli bu.

**Sorun:** `IInteractable` şu an parametresiz (`InteractionPrompt`, `Interact()`), bu yüzden her istasyon `PlayerHands.Instance` singleton'ına soruyor → co-op'ta hep 1. oyuncuyu bulur. Etkilenen dosyalar: `GlassShelf`, `TeaStove`, `Sink`, `Table`, `StorageRack`, `ComputerStation`, `GamblingTable`, `MafiaCollector`, `PropertyBuilding`, `HoldableItem`, `PoliceOfficer`, `PlayerInteractor`.

**Plan (henüz onaylanmadı):**
1. `PlayerContext` — etkileşimi yapan oyuncunun ellerini/kilidini/transform'unu taşıyan küçük sınıf, Player objesinde durur.
2. `IInteractable` → `string GetPrompt(PlayerContext)` + `void Interact(PlayerContext)`.
3. `PlayerInteractor` kendi `PlayerContext`'ini bulup her çağrıda geçirir.
4. İstasyonlar `PlayerHands.Instance` yerine `context.Hands` kullanır.
5. `PlayerHands` ve `PlayerLock` **singleton olmaktan çıkar**; dünya manager'ları singleton kalır.
6. **Editor işi yok, davranış birebir aynı kalmalı** — regresyon testi: Faz 1-4 akışlarının tamamı tekrar denenir.

---

## 10. YOL HARİTASI — YAPILANLAR VE KALANLAR

### ✅ FAZ 1 — Temel işletme döngüsü (BİTTİ)
| Adım | İçerik |
|---|---|
| 1 | `ItemData` (SO), `EconomyManager`, `ReputationManager`, `MoneyUI` |
| 2 | `PlayerController` (FPS), `IInteractable`, `PlayerInteractor`, `InteractionPromptUI` |
| 3a | `HoldableItem`, `PlayerHands`, `TeaGlass`, `GlassShelf`, `HeldItemUI` |
| 3b | `TimedStation`, `TeaStove`, `Sink`, `ProgressBarUI` — bulaşık döngüsü kapandı |
| 4a | `Table`, `TableManager`, `Customer`, `CustomerSpawner`, `OrderBubbleUI`, `Billboard` |
| 4b | Servis + para + itibar, kirli bardak masada kalır, `ReputationUI` |

### ✅ FAZ 2 — Stok, PC, büyüme (BİTTİ)
| Adım | İçerik |
|---|---|
| 5a | `InventoryManager`, `CayYapragi`, ocakta malzeme tüketimi, `StockUI` |
| 5b | `ShopCatalog`, `PurchaseManager`, `PlayerLock`, `ComputerStation`, `ComputerUI` |
| 5c | `DeliveryManager`, `DeliveryBox`, `StorageRack` — kurye koliyi kapıya bırakır |
| 5d | `UpgradeData/Catalog/Manager/Unlocker`, `TabGroupUI`, sekmeli PC paneli, `RowPrefabBuilder` |
| 5e | Kahve + Oralet, `MenuManager`, müşteri menüden rastgele sipariş, kilitli malzeme |
| 5f | İstasyon hız çarpanı, `CustomerModifiers` — sanayi ocağı ve okey takımı upgrade'leri |

### ✅ ALTYAPI (BİTTİ)
| Adım | İçerik |
|---|---|
| 6a | `DayCycleManager` — 24 saat kesintisiz gün, gece hızlandırma, son müşteri saati, kapanışta çıkış; `ClockUI` |
| 6b | `DailyStatsManager`, `DailyReportUI` — gün sonu muhasebe ekranı |
| 6c | `ISaveable`, `SaveManager`, `ItemDatabase` — JSON kayıt/yükleme, PC'de Kaydet butonu |

### ✅ FAZ 3 — Mahalle ve emlak (BİTTİ)
| Adım | İçerik |
|---|---|
| 7a | `PropertyData/Catalog/Manager`, PC'de EMLAK sekmesi, günlük kira, **itibar barajı** |
| 7b | Sokak + 3 bina, `PropertyBuilding` (yerinde satın alma), duvara asılı SATILIK tabelaları |

### ✅ FAZ 4 — İllegal işler (İSKELET BİTTİ)
| Adım | İçerik |
|---|---|
| 8a | `GizliKumarhaneUpgrade` (itibar 50), `GamblingTable` (sadece gece), `SuspicionManager` |
| 8b | `MafiaManager` — itibar+servet barajı, haraç, ödeme/ret, ret cezası ve ×1.5 zam |
| 8c | `PoliceManager` — şüpheye bağlı baskın, uyarı → memur → denetim; **örtü ile gizleme** |

---

### ⏳ KALANLAR — KARARLAŞTIRILMIŞ SIRA (2026-10-10)

> **Not:** 9a-1 (gerçek değerler + `BalancePresetApplier` + `BalanceLogger`) bitti.
> **Ölçümlü oynama (9a adım 2) kullanıcı tarafından ATLANDI** — "üşendim" dedi.
> Karar: formal ölçüm beklenmez, **bölüm 9'daki kağıt üstü tahminler** üzerinden
> denge düzeltmesi yapılır. Tahminler iki gerçek sorunu zaten tespit etmiş durumda:
> itibar barajları fiilen kilit değil, ve polis cezası sonrası **iflas kilidi** riski.

| # | Adım | Neden bu sırada |
|---|---|---|
| ~~**9a-3**~~ ✅ | Denge düzeltmeleri (kağıt tahminlere göre) — **bitti** | İtibar barajları ve **iflas kilidi** gerçek sorunlar; ucuz ve küçük. Oynanabilirliğin önkoşulu. |
| ~~**9b**~~ ✅ | **Co-op hazırlığı: etkileşim katmanı refactor** — **bitti** | `PlayerHands.Instance` tüm etkileşimlerin tek engeli. Şu an 12 çağrı noktası var, **her yeni istasyonda artıyor**. Şimdi ucuz, sonra pahalı. Oyuncu tarafında Editor işi yok. |
| ~~**9a-2**~~ ✅ | **Kumarhane işletmesi: NPC'ler oynar, oyuncu kasadır** — **bitti** | En büyük oynanış değişimi. **9b'den SONRA** yapılır ki yeni istasyon/NPC kodu doğuştan co-op uyumlu olsun. |
| ~~**10a-1**~~ ✅ | Hikaye giriş ekranı — **bitti** | Oyuna kimlik katar, küçük iş |
| ~~**10a-2a**~~ ✅ | Mahalle fısıltıları (olaylara eşlik eden ton satırı + ilk sabah yönlendirmesi) — **bitti** | Ucuz, kimliği oyunun içine taşır |
| ~~**10a-2b**~~ ✅ | Mevcut metinlerin tonu (sadece tablodakiler), `ArayuzMetinleri` + `Metinleri Uygula` — **bitti** | Fısıltılarla aynı tonu mevcut arayüze yayar |
| ~~**10c**~~ ✅ | Müşteri ve stok çeşitliliği — **bitti** (10c-1 profiller, 10c-2 masa oyunları, 10c-3 sahlep; 10c-4 bahçe → D bölümüne taşındı) | Faz 2'den kalan |
| **D1–D9** | **DÜKKAN DÜZENLEME — DEVAM EDİYOR** (D1 ✅ tepsi, D2 ✅ eşya altyapısı, D3 ✅ yerleştirme modu; sıradaki D4 eşya satın alma): tepsi, eşya altyapısı, serbest yerleştirme, eşya satın alma, her şey taşınabilir, duvarlar + sıralı genişletme + kumar odası, grup müşteriler, masa bazlı oyunlar, kişiselleştirme. Ayrıntı bölüm 6.9 | **Mahalleden önce:** dükkanın en büyük boyutu belli olsun ki mahalle onun etrafına kurulsun |
| **10d** | Mahalle planlaması, mülkleri işletmeye çevirme, kapı/iç-dış ayrımı, **Amca NPC + bitiş koşulu** | Mekanikler oturmadan şehir planı yapmak boşa emek |
| **10e** | "Aranan adam" senaryosu + mafya işbirliği kolu | Polis/mafya sistemlerine derinlik; altyapı hazır |
| **11** | Görsel cila, ses, ana menü, kayıt slotları | **En sona**: mekanikler değişince geometri/model yeniden yapılır |
| **FAZ 5** | **Co-op netcode** | Tek oyunculu her şey bitince |

**Sıralamanın mantığı:** önce oyunu oynanabilir kıl (9a-3), sonra ucuz-ve-koruyucu mimari hamlesi (9b), sonra en büyük oynanış değişimi (9a-2), sonra kimlik ve içerik (10a-e), en sonda cila (11) ve netcode (Faz 5).

**Teknik temizlik (fırsat buldukça):** 6 Editor builder'da kopyala-yapıştır kod var, ortak bir `SignFactory` yardımcısına çıkarılabilir.

---

## 10.5 GIT / GITHUB DURUMU

| | |
|---|---|
| Repo | `https://github.com/MetinKizil/Kahvehane_Simulation` — **PUBLIC** (herkese açık) |
| Remote | `origin` → yukarıdaki adres, `main` dalı `origin/main`'i takip ediyor |
| Yerel repo kökü | `D:\KAHVE DENEME\KAHVEHANEDENEME` (Unity proje klasörü) |
| Git kimliği | `MetinKizil` / `metinkizil210@gmail.com` |
| Kurulu araçlar | Git 2.55 + GitHub CLI 2.101 (winget ile kuruldu, PATH'e elle eklenmesi gerekebilir) |
| Giriş | `gh` ile `MetinKizil` hesabı (keyring), yetkiler `repo`, `workflow`...; `gh auth setup-git` ile git push da bu girişi kullanıyor |

**Durum (2026-10-17): GitHub bağlantısı kuruldu, tüm commit'ler push edildi** (ilk push: 17 commit, ~2.7 MB). Yerel `main` = `origin/main`.
```
(yeni)   Adim D3: yerlestirme modu - elde hayalet, tek tek tasima
a449009  Adim D2: esya altyapisi - FurnitureManager, kayit, oyun ici NavMesh
65b19d0  Adim D1: tepsi - 8 bardaklik servis ve toplama
a56f83e  DEVAM.md: Dukkan Duzenleme bolumu - kullanici kararlari ve D1-D9 yol haritasi
a45a845  GitHub remote kuruldu, DEVAM/CLAUDE git durumu guncellendi
9b5038a  Adim 10c-3: sahlep ve genel icecek istasyonu builder'i
6da6fa5  Adim 10c-2: masa oyunlari ve ikinci tur siparis
0883c45  Adim 10c-1: musteri profilleri
971ad07  Adim 10a-2b: mevcut arayuz metinlerinin tonu
177e081  Adim 10a-2a: mahalle fisiltilari
2de9851  Adim 10a-1: hikaye giris ekrani
97b0438  Adim 9a-2: kumarhane isletmesi - NPC kumarbazlar ve NavMesh duzeltmesi
3a5287c  Adim 9b: co-op hazirligi - PlayerContext ve etkilesim katmani
2a83e0c  Adim 9a-3: iflas korumasi ve itibar barajlari
b8f1c67  Adim 9a-1: gercek denge degerleri, BalancePresetApplier ve BalanceLogger
3076ff4  DEVAM.md: git durumu ve proje dersleri bolumleri eklendi
f329349  Adim 8c: polis baskini ve gizleme mekanigi
f9dc2d3  Adim 8b: mafya ve harac sistemi
7e6d8e1  Kan Bagi - Faz 1-2-3 ve Faz 4 basi
```

**Kurallar:**
- Her tamamlanıp test edilen adımdan sonra DEVAM.md güncellenir, sonra **commit + `git push`**.
- ⚠️ **Commit mesajlarına Claude imzası (`Co-Authored-By`) EKLENMEZ**, PR açıklamalarına "Generated with Claude Code" yazılmaz. Kullanıcının açık talebi.
- Commit mesajları Türkçe ama ASCII (şapkasız).
- ⚠️ **Repo herkese açık:** commit'e şifre, token, kişisel dosya girmemeli. Push'tan önce `git status` ile bak; `DengeLog/`, `Library/` vb. `.gitignore`'da.
- Commit'ten önce sahnede **test değerleri kalmamalı** (`startingMoney: 250`) — yoksa GitHub'a test sahnesi gider.
- Giriş düşerse: `gh auth status` ile bak; gerekirse kullanıcı kendi terminalinde `gh auth login` yapar (token sohbete yazılmaz), sonra `gh auth setup-git`.
- PowerShell oturumunda PATH tazelemek gerekebilir:
  `$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")`

---

## 11. YENİ SOHBETTE İLK YAPILACAK

1. Bu dosyayı oku.
2. `Assets/_Project/Scripts/` altındaki kodları oku — mevcut API ve stile uy.
3. **Sıradaki adımın (D4 — Eşya satın alma: PC'de eşya kataloğu, alınca elde hayalet) planını anlat, onay al**, sonra kod yaz. Planda **oyuncunun ne göreceğini** açıkça yaz. Dükkan Düzenleme bölümünün kararları ve taslak adımları bölüm 6.9'da.

### Bu projede öğrenilen dersler (tekrar aynı hataya düşme)
- **UI'ı elle kurdurma.** Panel/satır/tabela gerekiyorsa `Assets/_Project/Scripts/Editor/` altına bir builder yaz, menüden üretilsin. Elle kurulan ilk PC paneli üst üste bindi ve baştan yapıldı.
- **Sahne objelerine elle koordinat verme.** Tabela yerleştirme üç denemede de yanlış çıktı; çözüm hedefin `Renderer.bounds`'undan hesaplayan bir Editor komutu oldu.
- **Billboard ayrımı:** insan üstündeki tabelalarda Billboard VAR (levha yazının arkasında, +Z), duvara/masaya sabit tabelalarda YOK (levha önde, −Z). Karıştırılırsa yazı görünmez veya ters döner.
- **Prefab yeniden üretilince** o prefab'ı referans alan Inspector alanları kopar → yeniden bağlanmalı; sahnedeki kopyalar silinip yeniden konmalı.
- **Panel kökü hep aktif kalmalı** (kapalı obje event dinleyemez); açılıp kapanan şey içerik çocuğu olmalı.
- **`TrySpendMoney`'den geçen her özel harcama** (kumar bahsi, haraç) `Expenses`'a da yazılır → kendi satırında gösterilecekse giderden düşülmeli, yoksa çift sayılır.
- **Gün sonu bekleyen sistemler** (`OnDayEnded` dinleyicisi) yoksa oyun sessizce kilitlenir — guard eklendi ama benzer beklemelerde dikkat.
- **Aynı manager'ı aynı objeye iki kez ekleme.** Singleton fazlasını sessizce yok eder ama hangisinin kalacağı belirsiz; Inspector'da yanlış kopyayı düzenlemek çok kolay (iki DayCycleManager yüzünden geceler hızlanmıyordu). `Gercek Degerleri Uygula` artık kopyaları temizliyor.
- **Kayıt dosyası başlangıç değerlerini ezer.** `Starting Money`'yi değiştirdiysen ama eski kayıt duruyorsa değişiklik görünmez → önce `Kan Bagi > Kaydi Sil`.
- **Denge değerlerini Inspector'da elle değiştirme** — `BalancePresetApplier`'ın `Settings` tablosuna yaz, menüden uygula. Değerler tek yerde ve izlenebilir kalır.
- **NavMesh'i elle bake etme.** Pasif (kilitli) eşyalar bake'e girmez, `NavMeshObstacle`'lı objeler de NavMeshSurface ayarı yüzünden girmezdi → NPC'ler eşyanın içinden/üstünden yürür. Her zaman `Kan Bagi > NavMesh'i Yeniden Bake Et` + sahneyi kaydet + `NavMesh Kontrol Et`. Yeni eşya kök obje olarak ve collider'lı eklenmeli ki komut onu bulsun.
- **Türkçe oyun metni koda yazılmaz** — ScriptableObject asset'ine yazılır (kod ASCII kuralı). TMP fontu Türkçe karakterleri gösteriyor.
- **Yeni içeceği elle bağlama** — 10 ayrı yere bağlanması gerekiyor, biri unutulursa (özellikle ürün veritabanı → kayıt) sessizce bozulur. Veri asset'lerini yaz, `BeverageStationBuilder.Stations` tablosuna satır ekle, `Icecek Istasyonlarini Kur` → kaydet → `NavMesh'i Yeniden Bake Et` → kaydet.
- **Arayüz metnini Inspector'da elle değiştirme** — `ArayuzMetinleri.asset`'e yaz, `Kan Bagi > Metinleri Uygula`. Bir builder prefab'ı yeniden ürettiyse bu komutu tekrar çalıştır, yoksa metinler ASCII'ye döner.
- **Commit'ten önce sahnede test değerleri kalmış mı bak** (`startingMoney: 250`, `dayDurationMinutes: 6`). 10a-2b testinden sonra sahne test değerleriyle kaydedilmişti; commit'ten önce `Gercek Degerleri Uygula` + kaydet yapıldı.
- **Builder menüsünü kod değişikliğinden hemen sonra çalıştırma** — Unity derlemeyi bitirmeden tıklanırsa ESKİ kodla kurar (10a-1'de panel eski boyutla kuruldu). Console temizlenene / derleme çubuğu kaybolana kadar bekle, gerekirse `Ctrl+R`.
- **Oturum yarıda kalırsa** (DEVAM.md güncellenmeden), yeni oturumda önce `git status` ile commitlenmemiş işi kontrol et — 9a-2 kodu böyle bulundu.
4. Kod bittikten sonra Unity Editor talimatlarını adım adım ver.
