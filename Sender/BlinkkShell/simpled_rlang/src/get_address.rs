pub fn get_address<T: ?Sized>(x: &T) -> *const T {
    x as *const T
}

pub fn print_address<T: ?Sized>(x: &T) {
    println!("{:p}", x);
}

pub mod prelude {
    pub use crate::{
        get_address,
        print_address,
    };
}