#!/bin/sh
# Qutre 3D — Linux installer for any reasonably modern distribution.
# Installs a self-contained build; no distro packages or root required.
set -eu

APP_ID="qutre3d"
APP_NAME="Qutre 3D"

HERE=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
LANG_CODE=""
MODE="auto"
PREFIX=""
ACTION="install"

usage() {
    cat <<EOF
Qutre 3D installer

Usage: ./install.sh [options]

  --lang ru|en     interface language for the first launch
  --user           install into ~/.local (default for normal users)
  --system         install into /opt and /usr/local (requires root/sudo)
  --prefix DIR     custom application directory
  --uninstall      remove an existing installation
  -h, --help       show this help
EOF
}

while [ $# -gt 0 ]; do
    case "$1" in
        --lang)
            [ $# -ge 2 ] || { echo "Missing value for --lang"; exit 1; }
            LANG_CODE="$2"; shift 2 ;;
        --lang=*)
            LANG_CODE="${1#*=}"; shift ;;
        --user)
            MODE="user"; shift ;;
        --system)
            MODE="system"; shift ;;
        --prefix)
            [ $# -ge 2 ] || { echo "Missing value for --prefix"; exit 1; }
            PREFIX="$2"; shift 2 ;;
        --prefix=*)
            PREFIX="${1#*=}"; shift ;;
        --uninstall)
            ACTION="uninstall"; shift ;;
        -h|--help)
            usage; exit 0 ;;
        *)
            echo "Unknown option: $1"; usage; exit 1 ;;
    esac
done

if [ "$ACTION" = "uninstall" ]; then
    if [ "$MODE" = "auto" ]; then
        if [ "$(id -u)" = "0" ]; then MODE="system"; else MODE="user"; fi
    fi

    if [ "$MODE" = "system" ]; then
        APP_DIR="${PREFIX:-/opt/$APP_ID}"
    else
        DATA_HOME="${XDG_DATA_HOME:-$HOME/.local/share}"
        APP_DIR="${PREFIX:-$DATA_HOME/$APP_ID}"
    fi

    if [ -x "$APP_DIR/uninstall.sh" ]; then
        exec "$APP_DIR/uninstall.sh"
    fi

    echo "Qutre 3D is not installed in $APP_DIR"
    exit 1
fi

if [ ! -x "$HERE/Qutre3D" ]; then
    echo "Qutre3D binary not found next to install.sh"
    echo "Unpack the whole archive and run ./install.sh from that folder."
    exit 1
fi

if [ "$MODE" = "auto" ]; then
    if [ "$(id -u)" = "0" ]; then MODE="system"; else MODE="user"; fi
fi

# --- язык -------------------------------------------------------------------
if [ -z "$LANG_CODE" ]; then
    LANG_CODE="${QUTRE_LANG:-}"
fi

if [ -z "$LANG_CODE" ]; then
    if [ -t 0 ]; then
        printf '\n'
        printf '  Choose language / Выберите язык:\n'
        printf '    1) Русский\n'
        printf '    2) English\n'
        printf '  > '
        read -r answer || answer=1
        case "$answer" in
            2|e|E|en|EN|English|english) LANG_CODE="en" ;;
            *) LANG_CODE="ru" ;;
        esac
    else
        case "${LC_ALL:-${LANG:-}}" in
            ru*|RU*) LANG_CODE="ru" ;;
            *) LANG_CODE="en" ;;
        esac
    fi
fi

case "$LANG_CODE" in
    ru|en) ;;
    *) echo "Language must be 'ru' or 'en'"; exit 1 ;;
esac

# --- каталоги ---------------------------------------------------------------
if [ "$MODE" = "system" ]; then
    if [ "$(id -u)" != "0" ]; then
        echo "System install requires root. Re-run with: sudo ./install.sh --system"
        exit 1
    fi

    APP_DIR="${PREFIX:-/opt/$APP_ID}"
    BIN_DIR="/usr/local/bin"
    DESKTOP_DIR="/usr/share/applications"
    ICON_BASE="/usr/share/icons/hicolor"
    MIME_DIR="/usr/share/mime/packages"
else
    DATA_HOME="${XDG_DATA_HOME:-$HOME/.local/share}"
    APP_DIR="${PREFIX:-$DATA_HOME/$APP_ID}"
    BIN_DIR="$HOME/.local/bin"
    DESKTOP_DIR="$DATA_HOME/applications"
    ICON_BASE="$DATA_HOME/icons/hicolor"
    MIME_DIR="$DATA_HOME/mime/packages"
fi

echo "Installing $APP_NAME to $APP_DIR ..."

mkdir -p "$APP_DIR"
for entry in "$HERE"/*; do
    [ -e "$entry" ] || continue
    name=$(basename "$entry")
    case "$name" in
        install.sh|uninstall.sh) continue ;;
    esac
    cp -R "$entry" "$APP_DIR/"
done

chmod 755 "$APP_DIR/Qutre3D"

# --- иконка и ярлык ---------------------------------------------------------
mkdir -p "$ICON_BASE/256x256/apps"
if [ -f "$HERE/assets/icon-256.png" ]; then
    cp "$HERE/assets/icon-256.png" "$ICON_BASE/256x256/apps/$APP_ID.png"
elif [ -f "$HERE/assets/logo.png" ]; then
    cp "$HERE/assets/logo.png" "$ICON_BASE/256x256/apps/$APP_ID.png"
fi

mkdir -p "$DESKTOP_DIR"
cat > "$DESKTOP_DIR/$APP_ID.desktop" <<EOF
[Desktop Entry]
Type=Application
Version=1.0
Name=Qutre 3D
Name[ru]=Qutre 3D
GenericName=3D World Editor
GenericName[ru]=3D-редактор миров
Comment=Atmospheric 3D world editor
Comment[ru]=Атмосферный 3D-редактор миров
Exec="$APP_DIR/Qutre3D" %f
Icon=$ICON_BASE/256x256/apps/$APP_ID.png
Terminal=false
Categories=Graphics;3DGraphics;
MimeType=application/x-qutre;
StartupWMClass=Qutre3D
EOF
chmod 644 "$DESKTOP_DIR/$APP_ID.desktop"

mkdir -p "$MIME_DIR"
cat > "$MIME_DIR/$APP_ID.xml" <<'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<mime-info xmlns="http://www.freedesktop.org/standards/shared-mime-info">
  <mime-type type="application/x-qutre">
    <comment>Qutre 3D scene</comment>
    <comment xml:lang="ru">Сцена Qutre 3D</comment>
    <glob pattern="*.qutre"/>
  </mime-type>
</mime-info>
EOF

mkdir -p "$BIN_DIR"
ln -sf "$APP_DIR/Qutre3D" "$BIN_DIR/$APP_ID"

command -v update-mime-database >/dev/null 2>&1 && \
    update-mime-database "$(dirname "$MIME_DIR")" || true

command -v update-desktop-database >/dev/null 2>&1 && \
    update-desktop-database "$DESKTOP_DIR" || true

# --- язык первого запуска ---------------------------------------------------
CONFIG_DIR=""
CONFIG_OWNER=""
if [ "$MODE" = "user" ]; then
    CONFIG_DIR="${XDG_CONFIG_HOME:-$HOME/.config}/Qutre3D"
elif [ -n "${SUDO_USER:-}" ] && command -v getent >/dev/null 2>&1; then
    user_home=$(getent passwd "$SUDO_USER" | cut -d: -f6)
    if [ -n "$user_home" ]; then
        CONFIG_DIR="$user_home/.config/Qutre3D"
        CONFIG_OWNER="$SUDO_USER"
    fi
fi

if [ -n "$CONFIG_DIR" ]; then
    mkdir -p "$CONFIG_DIR"
    if [ ! -f "$CONFIG_DIR/settings.json" ]; then
        printf '{\n  "Language": "%s",\n  "LastDirectory": ""\n}\n' "$LANG_CODE" \
            > "$CONFIG_DIR/settings.json"
        [ -n "$CONFIG_OWNER" ] && chown -R "$CONFIG_OWNER" "$(dirname "$CONFIG_DIR")" 2>/dev/null || true
    fi
fi

# --- удаление ---------------------------------------------------------------
cat > "$APP_DIR/uninstall.sh" <<EOF
#!/bin/sh
set -eu
if [ "$MODE" = "system" ] && [ "\$(id -u)" != "0" ]; then
    echo "Run with sudo to remove the system installation."
    exit 1
fi
rm -f "$BIN_DIR/$APP_ID"
rm -f "$DESKTOP_DIR/$APP_ID.desktop"
rm -f "$ICON_BASE/256x256/apps/$APP_ID.png"
rm -f "$MIME_DIR/$APP_ID.xml"
rm -rf "$APP_DIR"
command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$DESKTOP_DIR" || true
echo "Qutre 3D removed."
EOF
chmod 755 "$APP_DIR/uninstall.sh"

echo
echo "Done. Qutre 3D $LANG_CODE is installed."
echo "  app:     $APP_DIR/Qutre3D"
echo "  command: $APP_ID"
echo "  remove:  $APP_DIR/uninstall.sh"

case ":$PATH:" in
    *":$BIN_DIR:"*) ;;
    *)
        echo
        echo "Add $BIN_DIR to PATH to run 'qutre3d' from anywhere:"
        echo "  export PATH=\"$BIN_DIR:\$PATH\""
        ;;
esac
