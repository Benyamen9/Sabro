using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Historical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFigureCardFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "emblem",
                schema: "historical",
                table: "historical_figures",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "read_more_url",
                schema: "historical",
                table: "historical_figures",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "syriac_name",
                schema: "historical",
                table: "historical_figures",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            // Pre-fills the emblem the Owner approved for each figure on 2026-10-25 (a
            // traditional attribute where iconography has one, else one from the
            // figure's story). Keyed by name because figure ids differ between
            // environments; a name that is not in the roster simply matches nothing,
            // and a figure that already has an emblem is left alone. Figures not listed
            // keep the emblem of their role.
            migrationBuilder.Sql(
                """
                UPDATE historical.historical_figures AS f
                SET emblem = v.emblem
                FROM (VALUES
                    ('Aaron the Priest', 'FloweringRod'),
                    ('Abba Aregawi', 'Serpent'),
                    ('Abel son of Adam', 'Lamb'),
                    ('Abgar V of Edessa', 'HolyFaceCloth'),
                    ('Abigail wife of Nabal', 'BasketOfLoaves'),
                    ('Abraham son of Terah', 'Stars'),
                    ('Absalom son of David', 'Oak'),
                    ('Abu al-Barakat ibn Kabar', 'Lamp'),
                    ('Adam', 'TreeOfKnowledge'),
                    ('Ahab son of Omri', 'Vineyard'),
                    ('Ambrose of Milan', 'Beehive'),
                    ('Amos of Tekoa', 'PlumbLine'),
                    ('Andrew the Apostle', 'SaltireCross'),
                    ('Annas', 'Censer'),
                    ('Antony the Great', 'TauCross'),
                    ('Aquila of Pontus', 'Tent'),
                    ('Arius', 'Scroll'),
                    ('Athaliah', 'Crown'),
                    ('Augustine of Hippo', 'BurningHeart'),
                    ('Balaam son of Beor', 'Donkey'),
                    ('Barabbas', 'Chains'),
                    ('Bardaisan of Edessa', 'CelestialSphere'),
                    ('Bartholomew the Apostle', 'Knife'),
                    ('Baruch son of Neriah', 'ReedPen'),
                    ('Bathsheba daughter of Eliam', 'Crown'),
                    ('Belshazzar', 'WritingHand'),
                    ('Benjamin son of Jacob', 'SilverCup'),
                    ('Boaz of Bethlehem', 'Sandal'),
                    ('Caiaphas', 'TornRobe'),
                    ('Cain son of Adam', 'Plough'),
                    ('Caleb son of Jephunneh', 'GrapeCluster'),
                    ('Clement of Rome', 'Anchor'),
                    ('Daniel the Prophet', 'Lion'),
                    ('Daniel the Stylite', 'Pillar'),
                    ('David son of Jesse', 'Harp'),
                    ('Deborah the Prophetess', 'PalmTree'),
                    ('Delilah', 'Shears'),
                    ('Eleazar son of Aaron', 'Censer'),
                    ('Elijah the Tishbite', 'ChariotOfFire'),
                    ('Elisha son of Shaphat', 'Mantle'),
                    ('Ephrem the Syrian', 'Harp'),
                    ('Esau son of Isaac', 'BowlOfLentils'),
                    ('Esther the Queen', 'Sceptre'),
                    ('Eve', 'Fruit'),
                    ('Ezekiel son of Buzi', 'Wheels'),
                    ('Ezra the Scribe', 'TorahScroll'),
                    ('Gideon son of Joash', 'Fleece'),
                    ('Goliath of Gath', 'BronzeHelmet'),
                    ('Gregory of Narek', 'Book'),
                    ('Gregory the Great', 'Dove'),
                    ('Hagar the Egyptian', 'WaterSkin'),
                    ('Haman the Agagite', 'Gallows'),
                    ('Herodias', 'Platter'),
                    ('Hezekiah son of Ahaz', 'Sundial'),
                    ('Ignatius of Antioch', 'Lions'),
                    ('Isaac son of Abraham', 'Ram'),
                    ('Isaiah son of Amoz', 'BurningCoal'),
                    ('Ishmael son of Abraham', 'Bow'),
                    ('Jacob Baradaeus', 'SaddleCloth'),
                    ('Jacob of Serugh', 'Flute'),
                    ('Jacob son of Isaac', 'Ladder'),
                    ('James son of Zebedee', 'ScallopShell'),
                    ('James the Brother of the Lord', 'FullersClub'),
                    ('Jeremiah son of Hilkiah', 'Yoke'),
                    ('Jeroboam son of Nebat', 'GoldenCalf'),
                    ('Jerome', 'Lion'),
                    ('Jesse of Bethlehem', 'RootOfJesse'),
                    ('Jezebel daughter of Ethbaal', 'Window'),
                    ('Joab son of Zeruiah', 'Sword'),
                    ('Job of Edessa', 'Book'),
                    ('John of Damascus', 'Hand'),
                    ('John of Sinai', 'Ladder'),
                    ('John son of Zebedee', 'Eagle'),
                    ('John the Baptist', 'LambWithCross'),
                    ('Jonah son of Amittai', 'GreatFish'),
                    ('Jonathan son of Saul', 'Arrows'),
                    ('Joseph of Arimathea', 'LinenShroud'),
                    ('Joseph of Nazareth', 'FloweringStaff'),
                    ('Joseph son of Jacob', 'Coat'),
                    ('Joshua son of Nun', 'Shofar'),
                    ('Josiah son of Amon', 'Scroll'),
                    ('Judah son of Jacob', 'Lion'),
                    ('Judas Iscariot', 'MoneyBag'),
                    ('Lazarus of Bethany', 'Tomb'),
                    ('Leah daughter of Laban', 'Mandrakes'),
                    ('Lot son of Haran', 'PillarOfSalt'),
                    ('Luke the Evangelist', 'Ox'),
                    ('Lydia of Thyatira', 'PurpleCloth'),
                    ('Manasseh of Judah', 'Chains'),
                    ('Mani', 'Brush'),
                    ('Marcion of Sinope', 'Ship'),
                    ('Mark the Evangelist', 'WingedLion'),
                    ('Martha of Bethany', 'Ladle'),
                    ('Mary Magdalene', 'MyrrhJar'),
                    ('Mary of Bethany', 'JarOfNard'),
                    ('Mary the Mother of Jesus', 'Lily'),
                    ('Matthew the Apostle', 'WingedMan'),
                    ('Mesrop Mashtots', 'Alphabet'),
                    ('Micah the Morashtite', 'Star'),
                    ('Michael the Syrian', 'Book'),
                    ('Miriam the Prophetess', 'Tambourine'),
                    ('Mordecai son of Jair', 'Horse'),
                    ('Moses son of Amram', 'Tablets'),
                    ('Nebuchadnezzar', 'Statue'),
                    ('Nehemiah son of Hacaliah', 'CityWall'),
                    ('Nicodemus', 'NightLamp'),
                    ('Nimrod son of Cush', 'Tower'),
                    ('Noah son of Lamech', 'Ark'),
                    ('Paul of Tarsus', 'Sword'),
                    ('Pharaoh of the Exodus', 'ChariotWheel'),
                    ('Philip the Apostle', 'Loaves'),
                    ('Phinehas son of Eleazar', 'Spear'),
                    ('Polycarp of Smyrna', 'Flames'),
                    ('Pontius Pilate', 'Basin'),
                    ('Priscilla of Corinth', 'Tent'),
                    ('Rabban Bar Sauma', 'PilgrimStaff'),
                    ('Rachel daughter of Laban', 'Crook'),
                    ('Rebecca daughter of Bethuel', 'WaterJar'),
                    ('Ruth the Moabite', 'SheafOfBarley'),
                    ('Salome the Myrrh-bearer', 'MyrrhJar'),
                    ('Samson son of Manoah', 'Jawbone'),
                    ('Samuel son of Elkanah', 'HornOfOil'),
                    ('Sarah wife of Abraham', 'Tent'),
                    ('Saul son of Kish', 'Spear'),
                    ('Simeon the Righteous', 'Candle'),
                    ('Simeon the Stylite the Elder', 'Pillar'),
                    ('Simeon the Stylite the Younger', 'Pillar'),
                    ('Simon Magus', 'Coins'),
                    ('Simon Peter', 'Keys'),
                    ('Solomon son of David', 'Temple'),
                    ('Stephen the Protomartyr', 'Stones'),
                    ('Tamar wife of Er', 'SealAndCord'),
                    ('Tatian', 'GospelBook'),
                    ('Tekle Haymanot', 'Wings'),
                    ('Thecla of Iconium', 'Lions'),
                    ('Thomas Aquinas', 'Sun'),
                    ('Thomas the Apostle', 'BuildersSquare'),
                    ('Yared', 'Birds'),
                    ('Zechariah the Priest', 'Censer'),
                    ('Zipporah daughter of Jethro', 'FlintKnife')
                ) AS v(name, emblem)
                WHERE f.name = v.name AND f.emblem IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "emblem",
                schema: "historical",
                table: "historical_figures");

            migrationBuilder.DropColumn(
                name: "read_more_url",
                schema: "historical",
                table: "historical_figures");

            migrationBuilder.DropColumn(
                name: "syriac_name",
                schema: "historical",
                table: "historical_figures");
        }
    }
}
