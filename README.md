# Tubes1_KopSekopSekop
Tubes 1 IF2211 Strategi dan Algoritma

## Bot Permainan Robocode Tank Royale dengan Algoritma Berbasis Greedy
### 1. Bot Greedy by Distance (TrackerBot)
Memastikan selalu berada pada jarak yang aman dan optimal dalam menghadapi musuh _no matter what_ sehingga akan sulit di-_ram_ oleh musuh sehingga bisa surive (kecuali ketika terpojok ke dinding) dan menhindar sambil berbelok ketika menabrak musuh. Bot ini akan menembak ketika jaraknya cukup dekat sehingga mengurangi miss terutama ketika menggunakan peluru berkekuatan 3
### 2. Bot Greedy by Survival with Dodge and Ram
memfokuskan untuk bertahan hidup dan meninggikan point survival, tetapi memiliki pola perilaku yang berbeda ketika menyisakan satu musuh nantinya. Dodge pada strategi ini dilakukan dengan melakukan dancing, yaitu gerakan pola acak yang sulit ditebak dan bergerak cepat sehingga pergerakannya dapat menghindari berbagai peluru lawan. Pola perilaku yang berbeda saat musuh tersisa satu adalah mengunci satu musuh sebagai sasaran, kemudian fokus menyerangnya dengan damage ram dan fire
### 3. Greedy by Survival Ranking
Greedy by berfokus kepada poin yang didapatkan ketika berhasil bertahan hidup. Algoritma ini terinspirasi dari sifat interaksi antar atom bermuatan, di mana suatu atom akan terdorong untuk menjauhi atom yang sesama jenis (untuk kasus ini, bot akan menjauhi bot yang masih kuat, dinding, dan tengah arena) dan juga terdorong untuk mendekati atom yang berlawan jenis (bot yang sudah lemah). 
### 4. Greedy by Quick Damage
Berfokus melakukan damage ke musuh dengan cepat tanpa terlalu membahayakan diri sendiri. Algoritma ini akan menembak musuh paling dekatnya dan berjalan terus dari sisi arena ke sisi yang lain, seolah-olah bot berpantul pada dinding.

## Requirements
- Tidak ada, jalankan saja jar Tank Royale
- Pastikan menginstall dot net versi 6 atau 9 (disarankan keduanya) untuk menjalankan bot

## Langkah Compile
- Tidak ada, sudah tercompile ke jar Tank Royale

## Author 
Fajar Kurniawan	13523027
Rafizan Muhammad Syawalazmi	13523034
Salman Hanif	13523056
