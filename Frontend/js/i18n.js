const i18n = {
  en: {
    nav_dashboard: "Dashboard",
    nav_deposit: "Deposit",
    nav_withdraw: "Withdraw",
    nav_transfer: "Transfer",
    nav_transactions: "Transactions",
    nav_settings: "Settings",
    nav_chat: "AI Chat",
    nav_logout: "Logout",
    settings_title: "Settings",
    settings_profile: "Profile",
    settings_name: "Name",
    settings_email: "Email",
    settings_member_since: "Member Since",
    settings_last_login: "Last Login",
    settings_security: "Security",
    settings_current_password: "Current Password",
    settings_new_password: "New Password",
    settings_change_password: "Change Password",
    settings_preferences: "Preferences",
    settings_dark_mode: "Dark Mode",
    settings_language: "Français",
    settings_legal: "Legal",
    settings_privacy: "Privacy Policy",
    settings_terms: "Terms of Service",
    settings_danger_zone: "Danger Zone",
    settings_danger_text: "Permanently delete your account and all data.",
    settings_delete_account: "Delete Account",
    logout_title: "Log Out",
    logout_confirm_text: "Are you sure you want to log out?",
    logout_cancel: "Cancel",
    logout_confirm: "Log Out",
    delete_title: "Delete Account",
    delete_confirm: "Delete",
    session_stay: "Stay Logged In",
    empty_title: "Your financial journey starts here",
    empty_text: "Create your first account",
    empty_btn: "Create Account",
    login_subtitle: "Sign in to your account",
    login_email: "Email",
    login_password: "Password",
    login_forgot: "Forgot password?",
    login_btn: "Sign in",
    login_no_account: "Don't have an account?",
    login_register: "Register",
    forgot_title: "Reset Password",
    forgot_text:
      "Enter your email, we'll send you a reset link. Valid for 15 minutes.",
    forgot_cancel: "Cancel",
    forgot_submit: "Send Reset Link",
    register_subtitle: "Create your account",
    register_fullname: "Full Name",
    register_email: "Email",
    register_password: "Password",
    register_confirm_password: "Confirm Password",
    register_btn: "Register",
    register_have_account: "Already have an account?",
    register_signin: "Sign in",
    dashboard_title: "My Accounts",
    dashboard_account_type: "Account Type",
    dashboard_chequing: "Chequing",
    dashboard_savings: "Savings",
    dashboard_create_account: "Create Account",
    dashboard_spending_overview: "Spending Overview",
    dashboard_deposits: "Deposits",
    dashboard_withdrawals: "Withdrawals",
    dashboard_total_balance: "Total Balance",
    dashboard_net_flow: "Net Flow (30d)",
    deposit_title: "Deposit Funds",
    deposit_select_account: "Select Account",
    deposit_amount: "Amount",
    deposit_btn: "Deposit",
    deposit_loading: "Loading accounts...",
    withdraw_title: "Withdraw Funds",
    withdraw_btn: "Withdraw",
    transfer_title: "Transfer Funds",
    transfer_my_accounts: "My Accounts",
    transfer_external: "Send to Someone",
    transfer_from: "From Account",
    transfer_to: "To Account",
    transfer_recipient: "Recipient Account Number",
    transfer_enter_number: "Enter Account Number",
    transfer_btn: "Transfer",
    transfer_select: "Select account...",
    transactions_title: "Transactions",
    transactions_all: "All",
    transactions_deposits: "Deposits",
    transactions_withdrawals: "Withdrawals",
    transactions_transfers: "Transfers",
    transactions_none: "No transactions found",
    transactions_prev: "Previous",
    transactions_next: "Next",
    transactions_select_account: "Select Account",
    reset_subtitle: "Enter your new password",
    reset_new_password: "New Password",
    reset_confirm_password: "Confirm Password",
    reset_btn: "Reset Password",
    reset_back: "Back to Sign In",
    not_found_title: "Page Not Found",
    not_found_text:
      "The page you're looking for doesn't exist or has been moved.",
    not_found_btn: "Go to Dashboard",
    server_error_title: "Something Went Wrong",
    server_error_text:
      "Something unexpected happened. Please try again or return to the dashboard.",
    go_back: "← Go Back",
    go_to_dashboard: "Go to Dashboard",
    privacy_title: "Privacy Policy",
    privacy_updated: "Last updated: May 2026",
    privacy_h1: "1. Information We Collect",
    privacy_p1:
      "BankLite collects your full name, email address and encrypted password when you register. We also collect transaction data including deposits, withdrawals and transfers you make within the app.",
    privacy_h2: "2. How We Use Your Information",
    privacy_p2:
      "Your information is used solely to provide BankLite services such as managing your accounts and processing transactions. We do not sell, share or distribute your personal data to any third parties.",
    privacy_h3: "3. Data Security",
    privacy_p3:
      "All passwords are hashed using BCrypt. Authentication is handled via JWT tokens. Your data is stored securely and never transmitted without encryption.",
    privacy_h4: "4. Data Retention",
    privacy_p4:
      "Your data is retained as long as your account is active. You may delete your account at any time from the Settings panel, which permanently removes all your data.",
    privacy_h5: "5. Cookies",
    privacy_p5:
      "BankLite uses session storage for authentication tokens and local storage for user preferences such as dark mode. No third party cookies are used.",
    terms_title: "Terms of Service",
    terms_updated: "Last updated: May 2026",
    terms_h1: "1. Acceptance of Terms",
    terms_p1:
      "By using BankLite you agree to these terms. BankLite is a demonstration banking application built for portfolio purposes and does not handle real money.",
    terms_h2: "2. Use of Service",
    terms_p2:
      "BankLite is provided for demonstration and educational purposes only. All transactions, balances and accounts are simulated and do not represent real financial activity.",
    terms_h3: "3. Account Responsibility",
    terms_p3:
      "You are responsible for maintaining the confidentiality of your account credentials. BankLite is not liable for any unauthorized access resulting from your failure to secure your credentials.",
    terms_h4: "4. Prohibited Activities",
    terms_p4:
      "You agree not to attempt to exploit, hack or misuse the BankLite platform. Any attempt to compromise the security or integrity of the application is strictly prohibited.",
    terms_h5: "5. Limitation of Liability",
    terms_p5:
      "BankLite is a portfolio project and provides no financial guarantees or services. I am not responsible for any issues arising from use of this application.",
    terms_h6: "6. Changes to Terms",
    terms_p6:
      "These terms may be updated at any time. Continued use of BankLite constitutes acceptance of any changes.",
    chatbot_status: "BankLite Assistant",
    chatbot_placeholder: "Ask Alfred anything...",
    chatbot_greeting:
      "Hi! I'm Alfred, your BankLite assistant. How can I help you today?",
    chatbot_typing: "Alfred is typing...",
    chatbot_error: "Sorry, I couldn't process your request. Please try again.",
    placeholder_current_password: "Current Password",
    placeholder_min_8: "Min 8 Characters",
    error_email_password: "Please enter both email and password.",
    error_fill_all_fields: "Please fill in all fields.",
    error_fullname_letters: "Full name can only contain letters and spaces.",
    error_passwords_no_match: "Passwords do not match.",
    error_enter_email: "Please enter your email.",
    success_reset_link: "If that email exists, a reset link has been sent.",
    error_fill_both_fields: "Please fill in both fields.",
    success_password_changed: "Password changed successfully!",
    delete_account_text:
      "This will permanently delete your account and all data. This cannot be undone.",
    first_login: "First login",
    error_select_account: "Please select an account.",
    error_valid_amount: "Please enter a valid amount.",
    error_max_deposit: "Maximum deposit amount is $1,000,000.",
    error_occurred: "An error occurred. Please try again.",
    error_max_withdrawal: "Maximum withdrawal amount is $1,000,000.",
    error_enter_recipient: "Please enter a recipient account number.",
    error_select_destination: "Please select a destination account.",
    error_max_transfer: "Maximum transfer amount is $1,000,000.",
    error_same_account: "Cannot transfer to same account",
    error_invalid_token: "Invalid or missing reset token.",
    error_min_8_chars: "Password must be at least 8 characters.",
    success_password_reset:
      "Password reset successfully! Redirecting to login...",
    success_account_created: "Account created successfully!",
    label_today: "Today",
    label_yesterday: "Yesterday",
    label_no_transactions: "No transactions found",
    session_expires: "Your session expires in",
    success_deposited: "Successfully deposited",
    success_withdrew: "Successfully withdrew",
    success_transferred: "Successfully transferred",
    dashboard_welcome: "Welcome back,",
    page_of: "Page",
    page_of_total: "of",
    placeholder_email: "you@example.com",
    placeholder_fullname: "Full Name",
    landing_meta_description:
      "BankLite - A full-stack banking app built with C#, .NET 8, PostgreSQL, JavaScript, Docker, and CI/CD.",
    landing_og_title: "BankLite | Landing",
    landing_og_description:
      "A polished full-stack banking app with secure auth, real-time features, and bilingual support.",
    landing_page_title: "BankLite | Full-Stack Banking App",
    landing_change_language: "Change language",
    landing_sign_in: "Sign in",
    landing_stack_aria: "Technology stack",
  },

  fr: {
    nav_dashboard: "Accueil",
    nav_deposit: "Dépôt",
    nav_withdraw: "Retrait",
    nav_transfer: "Virement",
    nav_transactions: "Transactions",
    nav_settings: "Paramètres",
    nav_chat: "Chat IA",
    nav_logout: "Déconnexion",
    settings_title: "Paramètres",
    settings_profile: "Profil",
    settings_name: "Nom",
    settings_email: "E-mail",
    settings_member_since: "Inscrit depuis",
    settings_last_login: "Dernière connexion",
    settings_security: "Sécurité",
    settings_current_password: "Mot de passe actuel",
    settings_new_password: "Nouveau mot de passe",
    settings_change_password: "Modifier le mot de passe",
    settings_preferences: "Préférences",
    settings_dark_mode: "Mode sombre",
    settings_language: "Français",
    settings_legal: "Mentions légales",
    settings_privacy: "Politique de confidentialité",
    settings_terms: "Conditions d'utilisation",
    settings_danger_zone: "Zone à risque",
    settings_danger_text:
      "Supprimez définitivement votre compte et toutes vos données.",
    settings_delete_account: "Supprimer le compte",
    logout_title: "Se déconnecter",
    logout_confirm_text: "Êtes-vous sûr de vouloir vous déconnecter ?",
    logout_cancel: "Annuler",
    logout_confirm: "Se déconnecter",
    delete_title: "Supprimer le compte",
    delete_text:
      "Cette action supprimera définitivement votre compte et toutes vos données. Elle est irréversible.",
    delete_confirm: "Supprimer",
    session_stay: "Rester connecté",
    empty_title: "Votre parcours financier commence ici",
    empty_text: "Créez votre premier compte",
    empty_btn: "Créer un compte",
    login_subtitle: "Connectez-vous à votre compte",
    login_email: "E-mail",
    login_password: "Mot de passe",
    login_forgot: "Mot de passe oublié ?",
    login_btn: "Se connecter",
    login_no_account: "Vous n'avez pas de compte ?",
    login_register: "S'inscrire",
    forgot_title: "Réinitialiser le mot de passe",
    forgot_text:
      "Entrez votre adresse e-mail, nous vous enverrons un lien de réinitialisation. Valable pendant 15 minutes.",
    forgot_cancel: "Annuler",
    forgot_submit: "Envoyer le lien",
    register_subtitle: "Créez votre compte",
    register_fullname: "Nom Complet",
    register_email: "E-mail",
    register_password: "Mot de passe",
    register_confirm_password: "Confirmer le mot de passe",
    register_btn: "S'inscrire",
    register_have_account: "Vous avez déjà un compte ?",
    register_signin: "Se connecter",
    dashboard_title: "Mes Comptes",
    dashboard_account_type: "Type de compte",
    dashboard_chequing: "Courant",
    dashboard_savings: "Épargne",
    dashboard_create_account: "Créer un compte",
    dashboard_spending_overview: "Aperçu des dépenses",
    dashboard_deposits: "Dépôts",
    dashboard_withdrawals: "Retraits",
    dashboard_total_balance: "Solde total",
    dashboard_net_flow: "Flux net (30 jours)",
    deposit_title: "Déposer des fonds",
    deposit_select_account: "Sélectionner un compte",
    deposit_amount: "Montant",
    deposit_btn: "Déposer",
    deposit_loading: "Chargement des comptes...",
    withdraw_title: "Retirer des fonds",
    withdraw_btn: "Retirer",
    transfer_title: "Virement de fonds",
    transfer_my_accounts: "Mes Comptes",
    transfer_external: "Envoyer à quelqu'un",
    transfer_from: "Compte d'origine",
    transfer_to: "Compte de destination",
    transfer_recipient: "Numéro de compte du destinataire",
    transfer_enter_number: "Saisir le numéro de compte",
    transfer_btn: "Effectuer le virement",
    transfer_select: "Sélectionner un compte...",
    transactions_title: "Transactions",
    transactions_all: "Toutes",
    transactions_deposits: "Dépôts",
    transactions_withdrawals: "Retraits",
    transactions_transfers: "Virements",
    transactions_none: "Aucune transaction trouvée",
    transactions_prev: "Précédent",
    transactions_next: "Suivant",
    transactions_select_account: "Sélectionner un compte",
    reset_subtitle: "Saisir votre nouveau mot de passe",
    reset_new_password: "Nouveau mot de passe",
    reset_confirm_password: "Confirmer le mot de passe",
    reset_btn: "Réinitialiser le mot de passe",
    reset_back: "Retour à la connexion",
    not_found_title: "Page introuvable",
    not_found_text:
      "La page que vous recherchez n'existe pas ou a été déplacée.",
    not_found_btn: "Aller à l'accueil",
    server_error_title: "Une erreur s'est produite",
    server_error_text:
      "Un événement inattendu s'est produit. Veuillez réessayer ou retourner à l'accueil.",
    go_back: "← Retour",
    go_to_dashboard: "Aller à l'accueil",
    privacy_title: "Politique de confidentialité",
    privacy_updated: "Dernière mise à jour : mai 2026",
    privacy_h1: "1. Informations que nous collectons",
    privacy_p1:
      "BankLite collecte votre nom complet, votre adresse e-mail et votre mot de passe crypté lors de votre inscription. Nous collectons également des données relatives aux transactions, notamment les dépôts, les retraits et les virements que vous effectuez au sein de l'application.",
    privacy_h2: "2. Comment nous utilisons vos informations",
    privacy_p2:
      "Vos informations sont utilisées uniquement pour fournir les services BankLite, tels que la gestion de vos comptes et le traitement des transactions. Nous ne vendons, ne partageons ni ne distribuons vos données personnelles à des tiers.",
    privacy_h3: "3. Sécurité des données",
    privacy_p3:
      "Tous les mots de passe sont hachés à l'aide de BCrypt. L'authentification est gérée via des jetons JWT. Vos données sont stockées en toute sécurité et ne sont jamais transmises sans être cryptées.",
    privacy_h4: "4. Conservation des données",
    privacy_p4:
      "Vos données sont conservées tant que votre compte est actif. Vous pouvez supprimer votre compte à tout moment depuis le panneau Paramètres, ce qui supprime définitivement toutes vos données.",
    privacy_h5: "5. Cookies",
    privacy_p5:
      "BankLite utilise le stockage de session pour les jetons d'authentification et le stockage local pour les préférences utilisateur, telles que le mode sombre. Aucun cookie tiers n'est utilisé.",
    terms_title: "Conditions d'utilisation",
    terms_updated: "Dernière mise à jour : mai 2026",
    terms_h1: "1. Acceptation des conditions",
    terms_p1:
      "En utilisant BankLite, vous acceptez les présentes conditions. BankLite est une application bancaire de démonstration conçue à des fins de gestion de portefeuille et ne gère pas d'argent réel.",
    terms_h2: "2. Utilisation du service",
    terms_p2:
      "BankLite est fourni à des fins de démonstration et d'apprentissage uniquement. Toutes les transactions, tous les soldes et tous les comptes sont simulés et ne reflètent pas une activité financière réelle.",
    terms_h3: "3. Responsabilité relative au compte",
    terms_p3:
      "Il vous incombe de préserver la confidentialité de vos identifiants de connexion. BankLite ne saurait être tenu responsable de tout accès non autorisé résultant d'un manquement de votre part à la sécurisation de vos identifiants.",
    terms_h4: "4. Activités interdites",
    terms_p4:
      "Vous vous engagez à ne pas tenter d'exploiter, de pirater ou d'utiliser de manière abusive la plateforme BankLite. Toute tentative visant à compromettre la sécurité ou l'intégrité de l'application est strictement interdite.",
    terms_h5: "5. Limitation de responsabilité",
    terms_p5:
      "BankLite est un projet de portfolio et ne fournit aucune garantie ni aucun service financier. Je ne suis pas responsable des problèmes découlant de l'utilisation de cette application.",
    terms_h6: "6. Modifications des conditions",
    terms_p6:
      "Les présentes conditions peuvent être mises à jour à tout moment. La poursuite de l'utilisation de BankLite vaut acceptation de toute modification.",
    chatbot_status: "Assistant BankLite",
    chatbot_placeholder: "Demandez à Alfred...",
    chatbot_greeting:
      "Bonjour ! Je suis Alfred, votre assistant BankLite. Comment puis-je vous aider ?",
    chatbot_typing: "Alfred tape...",
    chatbot_error:
      "Désolé, je n'ai pas pu traiter votre demande. Veuillez réessayer.",
    placeholder_current_password: "Mot de passe actuel",
    placeholder_min_8: "Min 8 caractères",
    error_email_password:
      "Veuillez saisir votre adresse e-mail et votre mot de passe.",
    error_fill_all_fields: "Veuillez remplir tous les champs.",
    error_fullname_letters:
      "Le nom complet ne peut contenir que des lettres et des espaces.",
    error_passwords_no_match: "Les mots de passe ne correspondent pas.",
    error_enter_email: "Veuillez saisir votre adresse e-mail.",
    success_reset_link:
      "Si cette adresse e-mail existe, un lien de réinitialisation vous a été envoyé.",
    error_fill_both_fields: "Veuillez remplir les deux champs.",
    success_password_changed: "Mot de passe modifié avec succès !",
    delete_account_text:
      "Cette action supprimera définitivement votre compte et toutes vos données. Elle est irréversible.",
    first_login: "Première connexion",
    error_select_account: "Veuillez sélectionner un compte.",
    error_valid_amount: "Veuillez saisir un montant valide.",
    error_max_deposit: "Le montant maximum du dépôt est de 1 000 000 $.",
    error_occurred: "Une erreur s'est produite. Veuillez réessayer.",
    error_max_withdrawal: "Le montant maximal de retrait est de 1 000 000 $.",
    error_enter_recipient:
      "Veuillez saisir le numéro du compte du destinataire.",
    error_select_destination: "Veuillez sélectionner un compte de destination.",
    error_max_transfer: "Le montant maximal du virement est de 1 000 000 $.",
    error_same_account:
      "Impossible d'effectuer un virement vers le même compte",
    error_invalid_token: "Jeton de réinitialisation invalide ou manquant.",
    error_min_8_chars: "Le mot de passe doit comporter au moins 8 caractères.",
    success_password_reset:
      "Réinitialisation du mot de passe réussie ! Redirection vers la page de connexion...",
    success_account_created: "Compte créé avec succès !",
    label_today: "Aujourd'hui",
    label_yesterday: "Hier",
    label_no_transactions: "Aucune transaction trouvée",
    session_expires: "Votre session expire dans",
    success_deposited: "Dépôt de",
    success_withdrew: "Retrait de",
    success_transferred: "Virement de",
    dashboard_welcome: "Bon retour,",
    page_of: "Page",
    page_of_total: "sur",
    placeholder_email: "vous@exemple.com",
    placeholder_fullname: "Nom Complet",
    landing_meta_description:
      "BankLite - Une application bancaire full-stack développée avec C#, .NET 8, PostgreSQL, JavaScript, Docker et CI/CD.",
    landing_og_title: "BankLite | Accueil",
    landing_og_description:
      "Une application bancaire complète avec authentification sécurisée, fonctionnalités en temps réel et assistance bilingue.",
    landing_page_title: "BankLite | Application Bancaire Full-Stack",
    landing_change_language: "Changer la langue",
    landing_sign_in: "Se connecter",
    landing_stack_aria: "Pile technologique",
  },
};

function t(key) {
  const lang = localStorage.getItem("language") || "en";
  return i18n[lang][key] || i18n["en"][key] || key;
}

function applyTranslations() {
  const lang = localStorage.getItem("language") || "en";
  const titles = {
    "index.html": { en: "BankLite | Login", fr: "BankLite | Connexion" },
    "register.html": {
      en: "BankLite | Register",
      fr: "BankLite | Inscription",
    },
    "dashboard.html": {
      en: "BankLite | Dashboard",
      fr: "BankLite | Accueil",
    },
    "deposit.html": { en: "BankLite | Deposit", fr: "BankLite | Dépôt" },
    "withdraw.html": { en: "BankLite | Withdraw", fr: "BankLite | Retrait" },
    "transfer.html": { en: "BankLite | Transfer", fr: "BankLite | Virement" },
    "transactions.html": {
      en: "BankLite | Transactions",
      fr: "BankLite | Transactions",
    },
    "reset-password.html": {
      en: "BankLite | Reset Password",
      fr: "BankLite | Réinitialisation",
    },
    "404.html": {
      en: "BankLite | Page Not Found",
      fr: "BankLite | Page introuvable",
    },
    "500.html": {
      en: "BankLite | Server Error",
      fr: "BankLite | Erreur serveur",
    },
    "privacy.html": {
      en: "BankLite | Privacy Policy",
      fr: "BankLite | Confidentialité",
    },
    "terms.html": {
      en: "BankLite | Terms of Service",
      fr: "BankLite | Conditions d'utilisation",
    },
    "landing.html": {
      en: "BankLite | Full-Stack Banking App",
      fr: "BankLite | Application Bancaire Full-Stack",
    },
  };
  const page = window.location.pathname.split("/").pop() || "index.html";
  if (titles[page]) document.title = titles[page][lang] || titles[page].en;

  document.querySelectorAll("[data-i18n]").forEach((el) => {
    const key = el.getAttribute("data-i18n");
    const attr = el.getAttribute("data-i18n-attr");
    if (attr) {
      el.setAttribute(attr, t(key));
    } else {
      el.textContent = t(key);
    }
  });
}

function setLanguage(lang) {
  localStorage.setItem("language", lang);
  applyTranslations();
  document.documentElement.lang = lang === "fr" ? "fr" : "en";
}

document.addEventListener("DOMContentLoaded", applyTranslations);
if (
  document.readyState === "complete" ||
  document.readyState === "interactive"
) {
  applyTranslations();
}
