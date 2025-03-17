/*
* Interface Utilisateur pour le Convertisseur Unicode-32 <-> UTF-8
* 
*
* Description :
* Cette classe Program fournit une interface utilisateur interactive pour le convertisseur
* Unicode-32/UTF-8. Elle offre quatre modes principaux :
* 1. Mode interactif pour des conversions caractère par caractère
* 2. Mode fichier pour convertir des fichiers entiers
* 3. Création de fichiers de test Unicode-32
* 4. Création de fichiers de test UTF-8
*
* L'interface est conçue pour être intuitive et robuste, avec une gestion complète
* des erreurs et des retours utilisateur clairs.
*/

using System;
using System.Text;
using System.IO;

class Program
{
    /// <summary>
    /// Point d'entrée principal du programme.
    /// Affiche le menu principal et gère les choix de l'utilisateur.
    /// </summary>
    static void Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("\n=== Convertisseur Unicode-32 <-> UTF-8 ===");
            Console.WriteLine("1. Mode Interactif");      // Conversion caractère par caractère
            Console.WriteLine("2. Mode Fichier");         // Conversion de fichiers complets
            Console.WriteLine("3. Créer fichier Unicode-32"); // Création de fichiers test U32
            Console.WriteLine("4. Créer fichier UTF-8");  // Création de fichiers test UTF-8
            Console.WriteLine("5. Quitter");
            Console.Write("\nChoisissez un mode (1-5): ");

            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    RunInteractiveMode();
                    break;
                case "2":
                    RunFileMode();
                    break;
                case "3":
                    CreateU32File();
                    break;
                case "4":
                    CreateUtf8File();
                    break;
                case "5":
                    return;
                default:
                    Console.WriteLine("Option invalide. Veuillez réessayer.");
                    break;
            }
        }
    }

    /// <summary>
    /// Crée un fichier de test au format Unicode-32 (UTF-32 LE).
    /// Permet à l'utilisateur d'entrer des valeurs hexadécimales qui sont
    /// stockées comme des entiers 32 bits en little-endian.
    /// </summary>
    static void CreateU32File()
    {
        Console.Write("Entrez le nom du fichier à créer (avec extension .u32): ");
        string? fileName = Console.ReadLine();

        if (string.IsNullOrEmpty(fileName))
        {
            Console.WriteLine("Nom de fichier invalide.");
            return;
        }

        Console.WriteLine("\nEntrez les valeurs Unicode-32 en hexadécimal (un par ligne).");
        Console.WriteLine("Appuyez sur Entrée sans valeur pour terminer.");

        try
        {
            using (var writer = new BinaryWriter(File.Create(fileName)))
            {
                while (true)
                {
                    Console.Write("Valeur (hex): ");
                    string? input = Console.ReadLine();

                    if (string.IsNullOrEmpty(input))
                        break;

                    // Conversion de l'entrée hexadécimale en uint32
                    uint value = Convert.ToUInt32(input, 16);
                    writer.Write(BitConverter.GetBytes(value)); // Écriture en little-endian
                }
            }
            Console.WriteLine($"Fichier {fileName} créé avec succès.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
        }
    }

    /// <summary>
    /// Crée un fichier de test au format UTF-8.
    /// Permet à l'utilisateur d'entrer directement les octets UTF-8 en hexadécimal,
    /// utile pour tester des séquences UTF-8 spécifiques.
    /// </summary>
    static void CreateUtf8File()
    {
        Console.Write("Entrez le nom du fichier à créer (avec extension .utf8): ");
        string? fileName = Console.ReadLine();

        if (string.IsNullOrEmpty(fileName))
        {
            Console.WriteLine("Nom de fichier invalide.");
            return;
        }

        Console.WriteLine("\nEntrez les bytes UTF-8 en hexadécimal (séparés par des espaces).");
        Console.WriteLine("Appuyez sur Entrée sans valeur pour terminer.");

        try
        {
            using (var writer = new BinaryWriter(File.Create(fileName)))
            {
                while (true)
                {
                    Console.Write("Bytes (hex): ");
                    string? input = Console.ReadLine();

                    if (string.IsNullOrEmpty(input))
                        break;

                    // Traitement des bytes séparés par des espaces
                    string[] byteStrings = input.Split(' ');
                    foreach (string byteStr in byteStrings)
                    {
                        byte value = Convert.ToByte(byteStr, 16);
                        writer.Write(value);
                    }
                }
            }
            Console.WriteLine($"Fichier {fileName} créé avec succès.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
        }
    }

    /// <summary>
    /// Gère la conversion de fichiers complets entre Unicode-32 et UTF-8.
    /// Permet de choisir la direction de conversion et de spécifier les chemins
    /// des fichiers source et destination.
    /// </summary>
    static void RunFileMode()
    {
        Console.WriteLine("\n=== Mode Fichier ===");
        Console.WriteLine("1. Unicode-32 vers UTF-8");
        Console.WriteLine("2. UTF-8 vers Unicode-32");
        Console.Write("\nChoisissez une direction (1-2): ");

        string? directionChoice = Console.ReadLine();
        string mode = directionChoice == "1" ? "-u32toutf8" : "-utf8tou32";

        if (directionChoice != "1" && directionChoice != "2")
        {
            Console.WriteLine("Option invalide.");
            return;
        }

        Console.Write("\nEntrez le chemin du fichier d'entrée: ");
        string? inputFile = Console.ReadLine();

        if (string.IsNullOrEmpty(inputFile) || !File.Exists(inputFile))
        {
            Console.WriteLine("Fichier d'entrée invalide ou inexistant.");
            return;
        }

        Console.WriteLine("\nChoisissez l'option de sortie:");
        Console.WriteLine("1. Même répertoire que l'entrée");
        Console.WriteLine("2. Spécifier un chemin différent");
        Console.Write("Option (1-2): ");

        string? outputChoice = Console.ReadLine();
        string? outputFile;

        if (outputChoice == "1")
        {
            // Génération automatique du nom de fichier de sortie
            string extension = directionChoice == "1" ? ".utf8" : ".u32";
            outputFile = Path.Combine(
                Path.GetDirectoryName(inputFile) ?? "",
                Path.GetFileNameWithoutExtension(inputFile) + extension
            );
        }
        else
        {
            Console.Write("\nEntrez le chemin du fichier de sortie: ");
            outputFile = Console.ReadLine();
            if (string.IsNullOrEmpty(outputFile))
            {
                Console.WriteLine("Chemin de sortie invalide.");
                return;
            }
        }

        try
        {
            UnicodeConverter.ConvertFile(inputFile, outputFile, mode == "-u32toutf8");
            Console.WriteLine($"Conversion réussie. Fichier de sortie: {outputFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
        }
    }

    /// <summary>
    /// Mode interactif permettant de convertir des caractères individuels
    /// dans les deux sens (U32<->UTF8) avec affichage détaillé des résultats.
    /// </summary>
    static void RunInteractiveMode()
    {
        while (true)
        {
            Console.WriteLine("\n=== Mode Interactif ===");
            Console.WriteLine("1. Unicode-32 vers UTF-8");
            Console.WriteLine("2. UTF-8 vers Unicode-32");
            Console.WriteLine("3. Retour au menu principal");
            Console.Write("\nChoisissez une option (1-3): ");

            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ConvertU32ToUtf8();
                    break;
                case "2":
                    ConvertUtf8ToU32();
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("Option invalide. Veuillez réessayer.");
                    break;
            }
        }
    }

    /// <summary>
    /// Conversion interactive d'une valeur Unicode-32 vers UTF-8.
    /// Affiche les résultats en format hexadécimal et décimal.
    /// </summary>
    static void ConvertU32ToUtf8()
    {
        Console.WriteLine("\n--- Conversion Unicode-32 vers UTF-8 ---");
        Console.Write("Entrez une valeur Unicode-32 (en hexadécimal, ex: 20AC): ");
        string? input = Console.ReadLine();

        try
        {
            uint unicode32 = Convert.ToUInt32(input, 16);
            Console.WriteLine($"\nValeur Unicode-32: 0x{unicode32:X4} ({unicode32} en décimal)");

            byte[] utf8 = UnicodeConverter.Unicode32ToUtf8(unicode32);

            Console.Write("Résultat UTF-8 (bytes): ");
            foreach (byte b in utf8)
            {
                Console.Write($"0x{b:X2} ");
            }
            Console.WriteLine();
        }
        catch (FormatException)
        {
            Console.WriteLine("Erreur: Format hexadécimal invalide.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur: {ex.Message}");
        }
    }

    /// <summary>
    /// Conversion interactive d'une séquence UTF-8 vers Unicode-32.
    /// Accepte une séquence d'octets en hexadécimal et affiche le résultat
    /// en formats hexadécimal et décimal.
    /// </summary>
    static void ConvertUtf8ToU32()
    {
        Console.WriteLine("\n--- Conversion UTF-8 vers Unicode-32 ---");
        Console.WriteLine("Entrez les bytes UTF-8 (en hexadécimal, séparés par des espaces, ex: E2 82 AC): ");
        string? input = Console.ReadLine();

        try
        {
            string[] byteStrings = input?.Split(' ') ?? Array.Empty<string>();
            byte[] utf8Bytes = new byte[byteStrings.Length];

            for (int i = 0; i < byteStrings.Length; i++)
            {
                utf8Bytes[i] = Convert.ToByte(byteStrings[i], 16);
            }

            Console.Write("\nBytes UTF-8 entrés: ");
            foreach (byte b in utf8Bytes)
            {
                Console.Write($"0x{b:X2} ");
            }
            Console.WriteLine();

            int position = 0;
            uint unicode32 = UnicodeConverter.Utf8ToUnicode32(utf8Bytes, ref position);
            Console.WriteLine($"Résultat Unicode-32: 0x{unicode32:X4} ({unicode32} en décimal)");
        }
        catch (FormatException)
        {
            Console.WriteLine("Erreur: Format hexadécimal invalide.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur: {ex.Message}");
        }
    }
}