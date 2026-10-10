# Kan Bağı — Claude Code Talimatları

**Projenin güncel durumu, tamamlanan adımlar, sahne kurulumu ve yol haritası: [DEVAM.md](DEVAM.md).**
Bu dosya *nasıl çalışacağını* söyler; DEVAM.md *nerede olduğumuzu* söyler. Yeni bir göreve başlamadan ikisini de oku.

---

## 1. Çalışma akışı (pazarlıksız)

1. **İzinsiz kod yazma.** Önce ne yapacağını anlat, onay al, sonra yaz.
2. Kod bitince **Unity Editor'de kullanıcının yapması gerekenleri adım adım** tarif et. Sahneyi sen kuramazsın, kullanıcı kurar.
3. Kullanıcı "çalışıyor" deyince **DEVAM.md'yi güncelle**, sonra commit at.
4. **Adımları küçük tut.** Büyük adımı ikiye böl — kullanıcının açık tercihi.

## 2. Kod disiplini

Aşağıdaki dört ilke [andrej-karpathy-skills](https://github.com/multica-ai/andrej-karpathy-skills) rehberinden uyarlanmıştır.

**Önce düşün:** Varsayımlarını söyle. Talep birden fazla şekilde okunabiliyorsa sessizce birini seçme, seçenekleri sun. Daha basit bir yol varsa önce onu öner.

**Basitlik:** Problemi çözen en az kodu yaz. İstenmeyen özellik, tek kullanımlık soyutlama, "belki gerekir" diye konfigürasyon, imkânsız durum için savunma kodu yazma.

**Cerrahi değişiklik:** Sadece talebin gerektirdiği yere dokun. Komşu kodu refactor etme, biçimlendirmeyi değiştirme, mevcut stile uy. İlgisiz ölü kodu silmek yerine *söyle*.

**Doğrulanabilir hedef:** Her adımın nasıl test edileceğini baştan belirt. Çok adımlı işte kısa bir plan ver ve her adımın doğrulama yöntemini yaz.

## 3. Yazılım anayasası

1. **SOLID** — sınıflar sadece kendi işini yapar, spagetti kod yasak.
2. **Observer Pattern** — sistemler `Action`/`UnityEvent` ile bağlanır. `Update` içinde başka script'i poll etmek **yasak**.
3. **Veri ScriptableObject'te** — ürün, fiyat, upgrade, mülk verileri koda gömülmez.
4. **UI mantıktan ayrı** — TextMeshPro; UI sadece event dinler ve gösterir, hesap yapmaz.
5. **Co-op uyumu** — nihai hedef eşli oynanış:
   - Oyuncuya özel durum (eller, kilit, bakış) **singleton olamaz**.
   - Dünya durumu (Economy, Inventory, Day, Upgrade, Property, Mafia, Police) singleton kalabilir — server-authoritative olacak.
   - Durum değişiklikleri **her zaman manager metodundan** geçer; bu metotlar ileride RPC sınırı olacak.
   - `Camera.main` ve `PlayerHands.Instance` gibi tekil erişimlerden kaçın.

## 4. Kod konvansiyonları

- Namespace: `KanBagi.<Alan>` (Core, Customers, Data, Delivery, Economy, Illegal, Interaction, Inventory, Items, Neighborhood, Player, Shop, UI, EditorTools)
- Yorumlar **Türkçe ama ASCII** — şapkasız, noktasız: `cay`, `musteri`, `siparis`. Kod içinde Türkçe karakter yok.
- Manager'lar: `public static X Instance { get; private set; }`
- Durum makineleri `Update` yerine **coroutine** ile.
- Inspector alanları mümkün olduğunca **opsiyonel**: boşsa kod otomatik bulur; bulamazsa **adresi net Türkçe `Debug.LogError`** basar ve `enabled = false` yapar.
- Kaydedilen sistemler `ISaveable` uygular; ScriptableObject referansları **asset adıyla** saklanır.

## 5. UI ve sahne işi — bu projenin en pahalı dersi

- **UI'ı kullanıcıya elle kurdurma.** Panel, satır, tabela veya NPC prefab'ı gerekiyorsa `Assets/_Project/Scripts/Editor/` altına bir builder yaz, Unity'nin `Kan Bagi` menüsünden tek tıkla üretilsin. Elle kurulan ilk PC paneli üst üste bindi ve baştan yapıldı.
- **Objelere elle koordinat verdirme.** Hedefin `Renderer.bounds`'undan hesaplayan bir Editor komutu yaz. Tabela yerleştirme üç denemede de yanlış çıktı; çözüm bu oldu.
- **Billboard ayrımı:** insan üstündeki tabelada Billboard **var** (levha yazının arkasında, +Z); duvara/masaya sabit tabelada **yok** (levha önde, −Z). Karıştırılırsa yazı görünmez.
- **Prefab yeniden üretilince** onu referans alan Inspector alanları kopar → yeniden bağlanmalı, sahnedeki kopyalar silinip yeniden konmalı.
- **Panel kökü hep aktif kalmalı** (kapalı obje event dinleyemez); açılıp kapanan şey içerik çocuğu olmalı.

## 6. Tekrar eden tuzaklar

- **`TrySpendMoney`'den geçen özel harcama** (kumar bahsi, haraç) `Expenses`'a da yazılır → kendi satırında gösterilecekse giderden düşülmeli, yoksa çift sayılır.
- **Gün sonu bekleyen sistem** yoksa (`OnDayEnded` dinleyicisi) oyun sessizce kilitlenir. Benzer beklemelerde guard koy.
- **Kayıt yüklendikten sonra** upgrade/mülk event'leri **yeniden duyurulmalı**, yoksa kilitler kapalı kalır.
- **`FindObjectsByType`** kilitli (pasif) objeleri bulmaz → gerekiyorsa `FindObjectsInactive.Include`.

## 7. Git

- Her tamamlanıp test edilen adımdan sonra DEVAM.md güncellenir, sonra commit.
- ⚠️ **Commit mesajlarına Claude imzası eklenmez** (`Co-Authored-By` yok, "Generated with Claude Code" yok). Kullanıcının açık talebi.
- Commit mesajları Türkçe ama ASCII.
- Repo: `https://github.com/MetinKizil/Kahvehane_Simulation` — push henüz yapılamadı, detay DEVAM.md bölüm 10.5'te. **Push'u çözmeye çalışmadan önce kullanıcıya sor.**
