# NeuralBridge.SQL

API d'analyse spectrale audio pilotée par IA. Elle permet d'analyser les fréquences d'un fichier audio, de comparer des signatures spectrales de référence, et de gérer l'historique des traitements — le tout en langage naturel.

---

## Stack

- **ASP.NET Core** — API REST
- **Semantic Kernel** — orchestration entre l'IA et le code C#
- **Mistral AI** — modèle de langage
- **PostgreSQL / Neon** — stockage des signatures et de l'historique

---

## Endpoint

```
POST /api/neural/ask
Content-Type: application/json

{ "prompt": "Combien de signatures spectrales sont enregistrées ?" }
```

---

## Comment ça fonctionne

L'utilisateur envoie une question en langage naturel. Mistral reçoit le prompt accompagné de la liste des fonctions C# disponibles (les `[KernelFunction]` du plugin). Si la question nécessite des données, Mistral ne répond pas directement — il indique quelle fonction appeler et avec quels arguments.

Semantic Kernel intercepte cet appel, exécute la vraie fonction C# (requête SQL, analyse audio, téléchargement…), puis renvoie le résultat à Mistral qui formule la réponse finale.

L'option `AutoInvokeKernelFunctions` est ce qui rend tout ça automatique : pas besoin de gérer manuellement le cycle appel/retour entre l'IA et le code.

---

## Fonctions disponibles

| Fonction | Description |
|---|---|
| `DownloadAudioFromYoutube` | Télécharge la piste audio d'une vidéo YouTube |
| `AnalyzeSpectralContent` | Analyse les 4 bandes fréquentielles d'un fichier audio (low / mid / high / air) |
| `SaveSpectralSignature` | Sauvegarde une signature spectrale en base |
| `GetSpectralSignatures` | Liste toutes les signatures de référence enregistrées |
| `SaveUserProcessing` | Enregistre un traitement dans l'historique utilisateur |
| `GetTableCount` | Retourne le nombre d'entrées dans une table |

---

## Configuration

Les secrets ne sont pas commités. En local, les définir via `dotnet user-secrets` dans le projet `NeuralBridge.API` :

```
MistralApiKey         → clé API Mistral
ConnectionStrings:NeonDb → connection string PostgreSQL Neon
```
