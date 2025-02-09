
# GTIN / EAN/ JAN / ISBN / ISMN


The [GTIN](https://en.wikipedia.org/wiki/Global_Trade_Item_Number) / 13-digit EAN is the most common code bar number.
Code bars can be searched on https://www.ean-search.org/

The GTIN/EAN-13 number consists of four components:
- GS1 "Country" prefix – 3 digits
- Manufacturer code – variable length
- Product code – variable length
- Check digit


The most common [GS1 "Country"](https://en.wikipedia.org/wiki/List_of_GS1_country_codes) prefixes:
- 001–099   UPC-A codes (Universal Product Code), USA
- 030-039	UPC-A - Drugs
- 040-049	UPC-A - Used to issue GS1 Restricted Circulation Numbers within a company
- 100–139   USA
- 300–379   France
- 400–440	Germany (440 from former RDA)
- 490–499	Japan (original Japanese Article Number range)
- 450–459	Japan (new Japanese Article Number range)
- 460–469	Russia (barcodes inherited from the Soviet Union)
- 500–509	United Kingdom
- 680–681	China
- 690–699	China
- 760–769	Switzerland and Liechtenstein
- 880–881	South Korea
- 977       Serial publications (ISSN)
- 978 979   Bookland (e.g. ISBN & ISMN numbers)
- 980    	Refundland (refund receipts)

Note: GS1 prefixes do NOT identify the country of origin / manufacture for a given product.

The GS1 "Company" prefix can be looked-up on [GEPIR](https://en.wikipedia.org/wiki/GEPIR) / [Verified by GS1](https://www.gs1.org/services/verified-by-gs1).

Examples Company codes:

- [Waitrose](https://www.gs1.org/services/verified-by-gs1/results?company_name=Waitrose&country=GB):
5000169, 50564430, 50564433, 50564434, 50564435, 50564437, 506081435, ..., 50700001485

Examples Product codes:

- Waitrose Ltd Waitrose Fairtrade Organic 6 Bananas in a bag <br/>
!! Referenced on https://www.ean-search.org/?q=5000169525524 !! <br/>
![](https://www.ean-search.org/barcode/5000169525524)

- Waitrose British Ox Cheek (0924) - 456gr - 11.50 £/kg - £5.24 <br/> !! Price is in barcode !! <br/>
![](https://www.ean-search.org/barcode/0231624005243)

- Waitrose Aberdeen Angus Bone Shin (1803) - 478gr - 10.50 £/kg - £5.02 <br/> !! Price is in barcode !! <br/>
![](https://www.ean-search.org/barcode/0243695005022)


# ISBN

The thirteen-digit number is divided into five parts of variable length, each part separated by a hyphen.
- The EAN Bookland prefix : 978 or 979
- Group or country identifier which identifies a national or geographic grouping of publishers;
- Publisher identifier which identifies a particular publisher within a group;
- Title identifier which identifies a particular title or edition of a title;
- Check digit is the single digit at the end of the ISBN which validates the ISBN.
(cf governance in https://www.isbn-international.org/content/isbn-governance)

Groups corresponds to language, region or countries.
The most common ones are :
- 978-0 and 978-1 : English, managed by https://www.isbn.org/
- 978-2	:   French, managed by https://www.afnil.org/  (details https://www.afnil.org/codebarre/)
- 978-3	:   German
- 978-4 :   Japan
- 978-5	:   former USSR
- 978-65 :  Brazil
- 978-7	:   China
- 978-80 :	former Czechoslovakia
- 978-81 :	India
- 978-82 :	Norway
- 978-88 :	Italy
- 978-89 :	South Korea
- 979-10 :  France
- 979-11 :  South Korea
- 979-12 :  Italy
- 979-13 :  Spain
- 979-8 :   United States of America

Full list on  https://en.wikipedia.org/wiki/List_of_ISBN_registration_groups.

Fun fact : United Kingdom has no dedicated registration group, nor ISBN agency.

Publishers lists (queried from https://grp.isbn-international.org/):
- https://en.m.wikipedia.org/wiki/List_of_group-0_ISBN_publisher_codes
- https://en.m.wikipedia.org/wiki/List_of_group-1_ISBN_publisher_codes

Fun fact [Wikipedia](https://en.wikipedia.org/wiki/ISBN#cite_note-21):<br/>
 Some books have several codes in the first block: e.g. A. M. Yaglom's Correlation Theory..., published by Springer Verlag, has two ISBNs, 0-387-96331-6 and 3-540-96331-6. Though Springer's 387 and 540 codes are different for English (0) and German (3); the same item number 96331 produces the same check digit for both (6). Springer uses 431 as the publisher code for Japanese (4), and 4-431-96331-? also has a check digit of 6. Other Springer books in English have publisher code 817, and 0-817-96331-? would also have a check digit of 6. This suggests that special considerations were made for assigning Springer's publisher codes, as random assignments of different publisher codes would not be expected to lead by coincidence to the same check digit every time for the same item number. Finding publisher codes for English and German, say, with this effect would amount to solving a linear equation in modular arithmetic.

## In France:

The main actors are :
- [CLIL](http://clil.org), Commission de liaison interprofessionnelle du livre.<br/>
La CLIL est l’Administrateur du Fichier Exhaustif du Livre (FEL), fichier commercial regroupant des fiches-produits homogènes et de qualité qui vise à faciliter à des professionnels la vente du livre, la recherche et l'identification des ouvrages à commander.
- [SNE](https://www.sne.fr/document/guide-pratique-du-fell-regles-pour-la-redaction-des-notices-bibliographiques/), Syndicat national de l’édition.<br/>
- [ALIRE](https://www.alire.asso.fr/le-numerique-pour-les-librairies/les-bases-de-donnees-bibliographiques/), Association des librairies informatisées et utilisatrices des réseaux électroniques.<br/>
Premier actionnaire de [Dilicom](https://www.dilicom.net/).

The main dataset is the FEL, "Fichier Exhaustif du livre". The Open data sources are available at
- [Nudger.fr](https://nudger.fr/opendata/isbn) ISBN France Open Data
- [Data.gouv.fr/datasets](https://www.data.gouv.fr/fr/datasets/base-de-codes-barres-noms-et-categories-produits/) Base de codes barres ISBN (400MB) & GTIN (3GB)
- [ISNI](https://isni.org/isni/0000000120186808) ISO certified global standard number for identifying the millions of contributors to creative works.
- [BNF dépôt légal](https://www.bnf.fr/fr/centre-d-aide/depot-legal-editeur-mode-demploi)

Example publishers and ISBN Prefixes:

- Arthaud   978-2-08, 978-2-7003Some
- Autrement     978-2-08, 978-2-7467, 978-2-86260
- Castor poche-Flammarion    978-2-08
- Flammarion    978-2-08
- Flammarion-Chat perché    978-2-08
- Flammarion-Jeunesse   978-2-08
- Père Castor-Flammarion    978-2-08
- Pygmalion     978-2-08, 978-2-7564, 978-2-85704
- Hatier 978-2-218, 978-2-401
- Ellipses-Edition  978-2-340, 978-2-7298
- Delcourt  978-2-413, 978-2-7560, 978-2-84055, 978-2-84789, 978-2-906187

Examples books ISBN / author ISSN:

- Boucle d'or et les trois ours <br/>
Flammarion Jeunesse Pere Castor <br/>
![](https://www.ean-search.org/barcode/9782081440357)

- Le nouveau Bescherelle - L'art de conjuguer <br/>
Hatier (Dépôt légal 1980) <br/>
![](https://www.ean-search.org/barcode/9782218048890)

- Le nouveau Bescherelle - La grammaire pour tous <br/>
Hatier (Dépôt légal 1984) <br/>
![](https://www.ean-search.org/barcode/9782218058912)

- Les contre-exemples en mathématiques <br/>
Editions Ellipses <br/>
![](https://www.ean-search.org/barcode/9782729834180)

- Notes Tome 01 - Born to be a larve <br/>
  Notes Tome 07 - Formicapunk <br/>
  [Boulet](https://isni.org/isni/0000000120186808) / Delcourt <br/>
![](https://www.ean-search.org/barcode/9782756014548) <br/>
![](https://www.ean-search.org/barcode/9782756031309) <br/>

- Yakitate! Ja-pan - un pain c'est tout t.1 à 3 <br/>
[Takashi Hashiguchi](https://isni.org/isni/0000000083823934) / Delcourt (Dépôt légal nov 2005, jan 2006 & mars 2006) <br/>
![](https://www.ean-search.org/barcode/9782847898583) <br/>
![](https://www.ean-search.org/barcode/9782847899429) <br/>
![](https://www.ean-search.org/barcode/9782756001043) (publisher prefix changed) <br/>

