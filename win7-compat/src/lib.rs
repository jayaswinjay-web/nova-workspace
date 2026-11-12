//! Windows 7 compatibility layer for the NOVA applications.
//!
//! Rust 1.78 and later build `std` for Windows 10 unless the `x86_64-win7-windows-msvc`
//! target is used, and `std` itself handles Windows 7 when it is. The dependencies above
//! `std`, though, still declare a handful of Windows 8/8.1 entry points as load-time imports.
//! On Windows 7 the loader stops with "entry point not found" before `main` runs, even when
//! the code would never call them.
//!
//! This crate defines those entry points itself. The linker then resolves the dependency's
//! references to these definitions instead of writing a DLL import, so the executable loads
//! on Windows 7. Each function forwards to the real system implementation when the running
//! Windows has one, so Windows 8, 8.1, 10 and 11 behave exactly as before; on Windows 7 it
//! does what the API would have done there.
//!
//! Only `main` needs to call [`install`], to make sure this object is linked in.
//!
//! The entry points covered: `CoIncrementMTAUsage`, `CoCreateFreeThreadedMarshaler` and
//! `CoTaskMemFree` (combase on Windows 8+), `RoGetActivationFactory` (WinRT), `PdfCreateRenderer`
//! (the in-box PDF renderer) and `PathCchStripPrefix` (the PathCch API set).
//!
//! The plain names are exported in release builds only. A raw-dylib import object holds the stubs
//! for a whole DLL, so when a debug build references any *other* combase, WinRT or
//! windows.data.pdf function the linker pulls that object in and it defines these six names too
//! (`LNK2005`, then `LNK1169`). Debug builds run on the build machine, which is Windows 10 or
//! later and has every one of these entry points, and the shipped executable is a release build;
//! so in debug the functions keep their mangled names (the crate's own tests still call them) and
//! the real imports are used. `cfg_attr` is the whole difference: a release build compiles the
//! same code it always did.

#![allow(non_snake_case)]
#![allow(clippy::missing_safety_doc)]

use std::ffi::c_void;
use std::sync::OnceLock;

type Hresult = i32;

const S_OK: Hresult = 0;
const E_NOTIMPL: Hresult = 0x8000_4001u32 as Hresult;
/// `REGDB_E_CLASSNOTREG`: the WinRT class is not registered on this system.
const REGDB_E_CLASSNOTREG: Hresult = 0x8004_0154u32 as Hresult;
/// `E_INVALIDARG`: one or more arguments is invalid (`0x80070057`).
const E_INVALIDARG: Hresult = 0x8007_0057u32 as Hresult;
/// `S_FALSE`: the call succeeded and did nothing (a `PathCch` function reports it when the path
/// has no prefix to strip).
const S_FALSE: Hresult = 1;

#[link(name = "kernel32")]
extern "system" {
    fn LoadLibraryW(name: *const u16) -> *mut c_void;
    fn GetProcAddress(module: *mut c_void, name: *const u8) -> *mut c_void;
}

/// A NUL-terminated UTF-16 literal.
macro_rules! wide {
    ($s:literal) => {{
        const W: &[u16] = &{
            let bytes = $s.as_bytes();
            let mut out = [0u16; $s.len() + 1];
            let mut i = 0;
            while i < bytes.len() {
                out[i] = bytes[i] as u16;
                i += 1;
            }
            out
        };
        W.as_ptr()
    }};
}

/// Resolves `name` in `dll`, or null when the DLL or the export is absent.
fn resolve(dll: *const u16, name: &'static [u8]) -> *mut c_void {
    // SAFETY: both strings are static and NUL-terminated; a failed load returns null, which
    // GetProcAddress is not called with.
    unsafe {
        let module = LoadLibraryW(dll);
        if module.is_null() {
            return std::ptr::null_mut();
        }
        GetProcAddress(module, name.as_ptr())
    }
}

fn cached(cell: &'static OnceLock<usize>, dll: *const u16, name: &'static [u8]) -> usize {
    *cell.get_or_init(|| resolve(dll, name) as usize)
}

// ---------------------------------------------------------------- combase.dll (Windows 8+)

/// `CoIncrementMTAUsage`: Windows 8 and later. Windows 7 has no MTA usage accounting and
/// COM works without it, so the call succeeds and reports a zero cookie.
#[cfg_attr(not(debug_assertions), no_mangle)]
pub unsafe extern "system" fn CoIncrementMTAUsage(cookie: *mut usize) -> Hresult {
    static REAL: OnceLock<usize> = OnceLock::new();
    let real = cached(&REAL, wide!("combase.dll"), b"CoIncrementMTAUsage\0");
    if real != 0 {
        let f: unsafe extern "system" fn(*mut usize) -> Hresult = std::mem::transmute(real);
        return f(cookie);
    }
    if !cookie.is_null() {
        *cookie = 0;
    }
    S_OK
}

/// `CoCreateFreeThreadedMarshaler`: a Windows 2000 API that moved from `ole32.dll` to
/// `combase.dll` in Windows 8. Both are tried, so the forwarding is exact everywhere.
#[cfg_attr(not(debug_assertions), no_mangle)]
pub unsafe extern "system" fn CoCreateFreeThreadedMarshaler(
    outer: *mut c_void,
    marshaler: *mut *mut c_void,
) -> Hresult {
    static REAL: OnceLock<usize> = OnceLock::new();
    static REAL_COMBASE: OnceLock<usize> = OnceLock::new();
    let real = cached(
        &REAL,
        wide!("ole32.dll"),
        b"CoCreateFreeThreadedMarshaler\0",
    );
    let real = if real != 0 {
        real
    } else {
        cached(
            &REAL_COMBASE,
            wide!("combase.dll"),
            b"CoCreateFreeThreadedMarshaler\0",
        )
    };
    if real == 0 {
        return E_NOTIMPL;
    }
    let f: unsafe extern "system" fn(*mut c_void, *mut *mut c_void) -> Hresult =
        std::mem::transmute(real);
    f(outer, marshaler)
}

/// `CoTaskMemFree`: `ole32.dll` on Windows 7, `combase.dll` from Windows 8.
#[cfg_attr(not(debug_assertions), no_mangle)]
pub unsafe extern "system" fn CoTaskMemFree(pv: *mut c_void) {
    static REAL: OnceLock<usize> = OnceLock::new();
    static REAL_COMBASE: OnceLock<usize> = OnceLock::new();
    let real = cached(&REAL, wide!("ole32.dll"), b"CoTaskMemFree\0");
    let real = if real != 0 {
        real
    } else {
        cached(&REAL_COMBASE, wide!("combase.dll"), b"CoTaskMemFree\0")
    };
    if real == 0 {
        return;
    }
    let f: unsafe extern "system" fn(*mut c_void) = std::mem::transmute(real);
    f(pv)
}

// ---------------------------------------------------------------- WinRT (Windows 8+)

/// `RoGetActivationFactory`: Windows 8 and later. Windows 7 has no Windows Runtime, so the
/// class is reported as not registered — exactly what a WinRT call fails with there. The
/// applications treat that as "this system setting is unavailable" and use their defaults.
#[cfg_attr(not(debug_assertions), no_mangle)]
pub unsafe extern "system" fn RoGetActivationFactory(
    activatable_class_id: *mut c_void,
    iid: *const c_void,
    factory: *mut *mut c_void,
) -> Hresult {
    static REAL: OnceLock<usize> = OnceLock::new();
    let real = cached(&REAL, wide!("combase.dll"), b"RoGetActivationFactory\0");
    if real != 0 {
        let f: unsafe extern "system" fn(*mut c_void, *const c_void, *mut *mut c_void) -> Hresult =
            std::mem::transmute(real);
        return f(activatable_class_id, iid, factory);
    }
    if !factory.is_null() {
        *factory = std::ptr::null_mut();
    }
    REGDB_E_CLASSNOTREG
}

// ---------------------------------------------------------------- windows.data.pdf (8.1+)

/// `PdfCreateRenderer`: the in-box Direct2D PDF renderer, Windows 8.1 and later. Windows 7
/// has no such renderer, so printing a PDF reports that it is not implemented; every other
/// print path is unaffected.
#[cfg_attr(not(debug_assertions), no_mangle)]
pub unsafe extern "system" fn PdfCreateRenderer(
    stream: *mut c_void,
    renderer: *mut *mut c_void,
) -> Hresult {
    static REAL: OnceLock<usize> = OnceLock::new();
    let real = cached(&REAL, wide!("windows.data.pdf.dll"), b"PdfCreateRenderer\0");
    if real != 0 {
        let f: unsafe extern "system" fn(*mut c_void, *mut *mut c_void) -> Hresult =
            std::mem::transmute(real);
        return f(stream, renderer);
    }
    if !renderer.is_null() {
        *renderer = std::ptr::null_mut();
    }
    E_NOTIMPL
}

// ---------------------------------------------------------------- pathcch (Windows 8+)

/// `PathCchStripPrefix`: Windows 8 and later, from the `api-ms-win-core-path-l1-1-0` API set.
/// Windows 7 has neither the API set nor the PathCch library, so a statically linked reference
/// would stop the loader. The work it does here is the documented one, done here instead: drop
/// the `\\?\` prefix from a path in place, and turn `\\?\UNC\server\share` into
/// `\\server\share`.
#[cfg_attr(not(debug_assertions), no_mangle)]
pub unsafe extern "system" fn PathCchStripPrefix(pszpath: *mut u16, cchpath: usize) -> Hresult {
    static REAL: OnceLock<usize> = OnceLock::new();
    let real = cached(
        &REAL,
        wide!("api-ms-win-core-path-l1-1-0.dll"),
        b"PathCchStripPrefix\0",
    );
    if real != 0 {
        let f: unsafe extern "system" fn(*mut u16, usize) -> Hresult = std::mem::transmute(real);
        return f(pszpath, cchpath);
    }
    if pszpath.is_null() {
        return E_INVALIDARG;
    }
    // The length of the string, capped at the buffer the caller declared.
    let mut len = 0usize;
    while len + 1 < cchpath && *pszpath.add(len) != 0 {
        len += 1;
    }
    let path = std::slice::from_raw_parts(pszpath, len);
    let prefix = match () {
        // `\\?\UNC\` (8 chars) becomes `\\`, so `UNC\` (4 chars) goes away.
        _ if starts_with(
            path,
            &[0x5C, 0x5C, 0x3F, 0x3F, 0x5C, 0x55, 0x4E, 0x43, 0x5C],
        ) =>
        {
            4
        }
        // `\\?\` (4 chars) goes away.
        _ if starts_with(path, &[0x5C, 0x5C, 0x3F, 0x3F, 0x5C]) => 4,
        _ => 0,
    };
    if prefix == 0 {
        // No prefix: the path is left as it is, and the call reports "nothing stripped".
        return S_FALSE;
    }
    let moved = path.len() - prefix;
    std::ptr::copy(pszpath.add(prefix), pszpath, moved);
    *pszpath.add(moved) = 0;
    S_OK
}

fn starts_with(path: &[u16], prefix: &[u16]) -> bool {
    path.len() >= prefix.len() && path[..prefix.len()] == *prefix
}

// ---------------------------------------------------------------- entry point

/// The running Windows version as `major * 10 + minor` (6.1 for Windows 7, 10.0 for Windows
/// 10 and 11), or 0 when it cannot be read.
///
/// `RtlGetVersion` is used rather than `GetVersionEx`: the documented call reports 6.2 unless
/// the executable carries a Windows 8.1+ compatibility manifest, while `RtlGetVersion` always
/// reports the truth.
pub fn windows_version() -> u32 {
    #[repr(C)]
    struct OsVersionInfoW {
        size: u32,
        major: u32,
        minor: u32,
        build: u32,
        platform: u32,
        csd: [u16; 128],
    }

    #[link(name = "ntdll")]
    extern "system" {
        fn RtlGetVersion(info: *mut OsVersionInfoW) -> i32;
    }

    let mut info = OsVersionInfoW {
        size: std::mem::size_of::<OsVersionInfoW>() as u32,
        major: 0,
        minor: 0,
        build: 0,
        platform: 0,
        csd: [0; 128],
    };
    // SAFETY: `info` is a correctly sized and aligned OSVERSIONINFOW.
    let status = unsafe { RtlGetVersion(&mut info) };
    if status < 0 {
        return 0;
    }
    info.major * 10 + info.minor
}

/// Links this crate into the executable. Call once, first thing in `main`.
pub fn install() {
    // Nothing to do: the shims above are the work. Touching them keeps the object alive in
    // builds that otherwise have nothing to reference from here.
    let _ = CoIncrementMTAUsage as *const () as usize;
    let _ = CoCreateFreeThreadedMarshaler as *const () as usize;
    let _ = CoTaskMemFree as *const () as usize;
    let _ = RoGetActivationFactory as *const () as usize;
    let _ = PdfCreateRenderer as *const () as usize;
    let _ = PathCchStripPrefix as *const () as usize;
}

/// Whether this Windows predates the Windows 10 graphics stack: the four applications then
/// render with OpenGL instead of DirectX 12.
pub fn needs_open_gl() -> bool {
    let v = windows_version();
    v != 0 && v < 100
}

#[cfg(test)]
mod tests {
    use super::*;

    /// A NUL-terminated UTF-16 buffer with `capacity` slots, from a Rust string.
    fn wide(s: &str, capacity: usize) -> Vec<u16> {
        let mut v: Vec<u16> = s.encode_utf16().collect();
        v.push(0);
        assert!(v.len() <= capacity, "{s} does not fit in {capacity}");
        v.resize(capacity, 0);
        v
    }

    fn text(buf: &[u16]) -> String {
        let end = buf.iter().position(|c| *c == 0).unwrap_or(buf.len());
        String::from_utf16_lossy(&buf[..end])
    }

    #[test]
    fn the_version_is_read() {
        // The build machine is Windows 10 or later.
        assert!(windows_version() >= 100);
        assert!(!needs_open_gl());
    }

    #[test]
    fn a_long_path_prefix_is_stripped_in_place() {
        // `\\?\` and `\\?\UNC\` go away; `\\?\UNC\server\share` keeps its leading `\\`.
        let mut buf = wide(r"\\?\C:\dir\file.txt", 64);
        assert_eq!(
            unsafe { PathCchStripPrefix(buf.as_mut_ptr(), buf.len()) },
            S_OK
        );
        assert_eq!(text(&buf), r"C:\dir\file.txt");

        let mut buf = wide(r"\\?\UNC\server\share", 64);
        assert_eq!(
            unsafe { PathCchStripPrefix(buf.as_mut_ptr(), buf.len()) },
            S_OK
        );
        assert_eq!(text(&buf), r"\\server\share");

        // No prefix: the path is left alone, the call reports S_FALSE as Windows' own does, and
        // the NUL terminator is still there.
        let path = r"C:\dir";
        let mut buf = wide(path, 64);
        assert_eq!(
            unsafe { PathCchStripPrefix(buf.as_mut_ptr(), buf.len()) },
            S_FALSE
        );
        assert_eq!(text(&buf), path);
        assert_eq!(buf[path.len()], 0);

        // A null pointer is the caller's error, not a crash.
        assert_eq!(
            unsafe { PathCchStripPrefix(std::ptr::null_mut(), 64) },
            E_INVALIDARG
        );
    }
}
