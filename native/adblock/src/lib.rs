use adblock::{Engine, lists::{FilterSet, ParseOptions}, request::Request};
use std::{ffi::{CStr, c_char}, panic::{catch_unwind, AssertUnwindSafe}, sync::{Mutex, OnceLock}};

static ENGINE: OnceLock<Mutex<Option<Engine>>> = OnceLock::new();

fn engine_slot() -> &'static Mutex<Option<Engine>> {
    ENGINE.get_or_init(|| Mutex::new(None))
}

#[no_mangle]
pub unsafe extern "C" fn lyravoxen_adblock_init(rules: *const c_char) -> i32 {
    catch_unwind(AssertUnwindSafe(|| {
        if rules.is_null() { return -1; }
        let Ok(text) = CStr::from_ptr(rules).to_str() else { return -2; };
        let mut set = FilterSet::new(false);
        set.add_filter_list(text.to_owned(), ParseOptions::default());
        let engine = Engine::new_with_filter_set(set);
        let Ok(mut slot) = engine_slot().lock() else { return -3; };
        *slot = Some(engine);
        1
    })).unwrap_or(-99)
}

#[no_mangle]
pub unsafe extern "C" fn lyravoxen_adblock_should_block(
    url: *const c_char, source_url: *const c_char, request_type: *const c_char, method: *const c_char
) -> i32 {
    catch_unwind(AssertUnwindSafe(|| {
        if url.is_null() || source_url.is_null() || request_type.is_null() || method.is_null() { return -1; }
        let (Ok(url), Ok(source), Ok(kind), Ok(method)) = (
            CStr::from_ptr(url).to_str(), CStr::from_ptr(source_url).to_str(),
            CStr::from_ptr(request_type).to_str(), CStr::from_ptr(method).to_str()
        ) else { return -2; };
        let Ok(slot) = engine_slot().lock() else { return -3; };
        let Some(engine) = slot.as_ref() else { return 0; };
        let Ok(request) = Request::new(url, source, kind, method) else { return 0; };
        if engine.check_network_request(&request).should_block() { 1 } else { 0 }
    })).unwrap_or(-99)
}

#[no_mangle]
pub extern "C" fn lyravoxen_adblock_ready() -> i32 {
    engine_slot().lock().map(|slot| if slot.is_some() { 1 } else { 0 }).unwrap_or(0)
}
